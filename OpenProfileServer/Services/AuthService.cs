using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Auth;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Utilities;

namespace OpenProfileServer.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ISystemSettingService _settingService;
    private readonly IVerificationService _verificationService;
    private readonly IEmailService _emailService;
    private readonly JwtOptions _jwtOptions;

    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    public AuthService(
        ApplicationDbContext context, 
        ITokenService tokenService, 
        ISystemSettingService settingService,
        IVerificationService verificationService,
        IEmailService emailService,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _settingService = settingService;
        _verificationService = verificationService;
        _emailService = emailService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ApiResponse<AuthConfigDto>> GetConfigAsync()
    {
        var allowReg = await _settingService.GetBoolAsync(SystemSettingKeys.AllowRegistration, false);
        var requireEmail = await _settingService.GetBoolAsync(SystemSettingKeys.RegistrationRequiresEmail, false);
        var emailServiceUp = _emailService.IsEnabled;

        return ApiResponse<AuthConfigDto>.Success(new AuthConfigDto
        {
            AllowRegistration = allowReg,
            RegistrationRequiresEmail = requireEmail && emailServiceUp
        });
    }

    public async Task<ApiResponse<MessageResponse>> SendCodeAsync(SendCodeRequestDto dto)
    {
        if (dto.Type == VerificationType.Registration)
        {
            if (await _context.Accounts.AnyAsync(a => a.Emails.Any(e => e.Email.ToLower() == dto.Email.ToLower())))
                return ApiResponse<MessageResponse>.Failure("Email is already in use.");
        }
        else if (dto.Type == VerificationType.VerifyEmail) 
        {
             if (await _context.Accounts.AnyAsync(a => a.Emails.Any(e => e.Email.ToLower() == dto.Email.ToLower())))
                return ApiResponse<MessageResponse>.Failure("Email is already in use.");
        }

        var sent = await _verificationService.SendCodeAsync(dto.Email, dto.Type);
        
        if (!sent) return ApiResponse<MessageResponse>.Failure("Could not send email. Please try again later or contact support.");
        
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Verification code sent."));
    }

    public async Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        // 1. Config Check
        var allowReg = await _settingService.GetBoolAsync(SystemSettingKeys.AllowRegistration, false);
        if (!allowReg) return ApiResponse<TokenResponseDto>.Failure("Registration is disabled.");

        var requireEmail = await _settingService.GetBoolAsync(SystemSettingKeys.RegistrationRequiresEmail, false);
        var emailServiceUp = _emailService.IsEnabled;
        
        // 2. Format Validation
        if (!AccountNameRegex.IsMatch(dto.AccountName))
            return ApiResponse<TokenResponseDto>.Failure("Invalid account name.");

        // 3. Uniqueness Check
        var accLower = dto.AccountName.ToLowerInvariant();
        if (await _context.Accounts.AnyAsync(a => a.AccountName.ToLower() == accLower))
            return ApiResponse<TokenResponseDto>.Failure("Account name taken.");

        if (await _context.AccountEmails.AnyAsync(e => e.Email.ToLower() == dto.Email.ToLower()))
            return ApiResponse<TokenResponseDto>.Failure("Email already in use.");

        // 4. Verification Code Check (Atomic)
        if (requireEmail && emailServiceUp)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return ApiResponse<TokenResponseDto>.Failure("Verification code is required.");

            var isValid = await _verificationService.ValidateCodeAsync(dto.Email, VerificationType.Registration, dto.Code);
            if (!isValid)
                return ApiResponse<TokenResponseDto>.Failure("Invalid or expired verification code.");
        }

        // 5. Create Account (Transaction)
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var accountId = Guid.NewGuid();
            var (hash, salt) = CryptographyProvider.CreateHash(dto.Password);

            var account = new Account
            {
                Id = accountId,
                AccountName = dto.AccountName,
                Type = AccountType.Personal,
                Role = AccountRole.User,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            var credential = new AccountCredential
            {
                AccountId = accountId,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            var email = new AccountEmail
            {
                AccountId = accountId,
                Email = dto.Email,
                IsPrimary = true,
                IsVerified = true, 
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var profile = new PersonalProfile { Id = accountId, Account = account, DisplayName = dto.AccountName };
            var settings = new PersonalSettings { Id = accountId, Account = account };
            var security = new AccountSecurity { AccountId = accountId };

            _context.Accounts.Add(account);
            _context.AccountCredentials.Add(credential);
            _context.AccountEmails.Add(email);
            _context.PersonalProfiles.Add(profile);
            _context.PersonalSettings.Add(settings);
            _context.AccountSecurities.Add(security);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 6. Auto-Login
            var tokenData = await GenerateTokenResponseInternalAsync(account, null);
            return ApiResponse<TokenResponseDto>.Success(tokenData, "Account created successfully.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginRequestDto dto, string? deviceInfo)
    {
        var account = await _context.Accounts
            .Include(a => a.Credential)
            .Include(a => a.Emails)
            .FirstOrDefaultAsync(a => a.AccountName == dto.Login || a.Emails.Any(e => e.Email == dto.Login && e.IsPrimary));
        
        if (account?.Credential == null || !CryptographyProvider.Verify(dto.Password, account.Credential.PasswordHash, account.Credential.PasswordSalt))
            return ApiResponse<TokenResponseDto>.Failure("Invalid credentials.");
            
        if (account.Status == AccountStatus.Banned) 
            return ApiResponse<TokenResponseDto>.Failure("Account banned.");
        
        var tokenData = await GenerateTokenResponseInternalAsync(account, deviceInfo);
        return ApiResponse<TokenResponseDto>.Success(tokenData);
    }
    
    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
         var tokenEntry = await _context.RefreshTokens
            .Include(t => t.Account).ThenInclude(a => a.Credential)
            .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);
            
         if (tokenEntry == null || tokenEntry.IsExpired) 
            return ApiResponse<TokenResponseDto>.Failure("Invalid token.");
            
         if (tokenEntry.Account.Status == AccountStatus.Banned) 
            return ApiResponse<TokenResponseDto>.Failure("Account revoked.");
            
         _context.RefreshTokens.Remove(tokenEntry);
         
         var tokenData = await GenerateTokenResponseInternalAsync(tokenEntry.Account, tokenEntry.DeviceInfo);
         return ApiResponse<TokenResponseDto>.Success(tokenData);
    }

    public async Task<ApiResponse<MessageResponse>> LogoutAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null) { _context.RefreshTokens.Remove(token); await _context.SaveChangesAsync(); }
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Logged out."));
    }

    public async Task<ApiResponse<MessageResponse>> LogoutAllDevicesAsync(Guid accountId)
    {
         var account = await _context.Accounts
            .Include(a => a.Credential)
            .Include(a => a.RefreshTokens)
            .FirstOrDefaultAsync(a => a.Id == accountId);
            
         if (account == null) return ApiResponse<MessageResponse>.Failure("Not found.");
         
         _context.RefreshTokens.RemoveRange(account.RefreshTokens);
         if (account.Credential != null) { account.Credential.SecurityStamp = Guid.NewGuid().ToString("N"); }
         
         await _context.SaveChangesAsync();
         return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Logged out everywhere."));
    }

    private async Task<TokenResponseDto> GenerateTokenResponseInternalAsync(Account account, string? deviceInfo)
    {
        var accessToken = _tokenService.GenerateAccessToken(account);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        var refreshTokenEntry = new RefreshToken
        {
            Token = refreshToken, 
            AccountId = account.Id, 
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays), 
            DeviceInfo = deviceInfo
        };
        
        _context.RefreshTokens.Add(refreshTokenEntry);
        account.LastLogin = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return new TokenResponseDto 
        { 
            AccessToken = accessToken, 
            RefreshToken = refreshToken, 
            ExpiresAt = _tokenService.GetAccessTokenExpiration() 
        };
    }
}
