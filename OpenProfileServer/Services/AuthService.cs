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
    private readonly JwtOptions _jwtOptions;

    // Regex for valid username: Alphanumeric, underscores, hyphens. 3-64 chars.
    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    public AuthService(
        ApplicationDbContext context, 
        ITokenService tokenService, 
        ISystemSettingService settingService,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _settingService = settingService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        // 1. Check if registration is allowed
        var allowRegistration = await _settingService.GetBoolAsync(SystemSettingKeys.AllowRegistration, false);
        if (!allowRegistration)
        {
            return ApiResponse<TokenResponseDto>.Failure("Public registration is currently disabled.");
        }

        // 2. Validate format
        if (!AccountNameRegex.IsMatch(dto.AccountName))
        {
            return ApiResponse<TokenResponseDto>.Failure("Account name contains invalid characters. Use letters, numbers, underscores, or hyphens.");
        }

        // 3. Check Uniqueness (Case-Insensitive)
        // Using ToLowerInvariant to ensure consistent comparison regardless of DB collation
        var accountNameLower = dto.AccountName.ToLowerInvariant();
        
        var existingUser = await _context.Accounts
            .AnyAsync(a => a.AccountName.ToLower() == accountNameLower);
            
        if (existingUser)
        {
            return ApiResponse<TokenResponseDto>.Failure("Account name is already taken.");
        }

        var existingEmail = await _context.AccountEmails
            .AnyAsync(e => e.Email.ToLower() == dto.Email.ToLower());
            
        if (existingEmail)
        {
            return ApiResponse<TokenResponseDto>.Failure("Email is already in use.");
        }

        // 4. Create Entities (Transaction)
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var accountId = Guid.NewGuid();
            var (hash, salt) = CryptographyProvider.CreateHash(dto.Password);

            // Core Account
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

            // Credentials
            var credential = new AccountCredential
            {
                AccountId = accountId,
                PasswordHash = hash,
                PasswordSalt = salt,
                UpdatedAt = DateTime.UtcNow
            };

            // Primary Email
            var email = new AccountEmail
            {
                AccountId = accountId,
                Email = dto.Email,
                IsPrimary = true,
                IsVerified = false, // Verification logic handled separately
                CreatedAt = DateTime.UtcNow
            };

            // Security Settings
            var security = new AccountSecurity
            {
                AccountId = accountId,
                UpdatedAt = DateTime.UtcNow
            };

            // Default Personal Profile
            var profile = new PersonalProfile
            {
                Id = accountId,
                Account = account,
                DisplayName = dto.AccountName, // Default to username
                Description = "Hello, I am using OpenProfile!",
            };

            // Default Settings
            var settings = new PersonalSettings
            {
                Id = accountId,
                Account = account,
                Visibility = Visibility.Public,
                DefaultVisibility = Visibility.Public,
                ShowLocalTime = false
            };

            _context.Accounts.Add(account);
            _context.AccountCredentials.Add(credential);
            _context.AccountEmails.Add(email);
            _context.AccountSecurities.Add(security);
            _context.PersonalProfiles.Add(profile);
            _context.PersonalSettings.Add(settings);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 5. Auto-Login
            return await GenerateTokenResponseAsync(account, null); 
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginRequestDto dto, string? deviceInfo)
    {
        // Unified error message to prevent username enumeration
        const string invalidMessage = "Invalid account name, email or password.";

        var account = await _context.Accounts
            .Include(a => a.Credential)
            .Include(a => a.Emails)
            .FirstOrDefaultAsync(a => a.AccountName == dto.Login || a.Emails.Any(e => e.Email == dto.Login && e.IsPrimary));

        // 1. Basic Existence and Credential Check
        if (account?.Credential == null) return ApiResponse<TokenResponseDto>.Failure(invalidMessage);

        if (!CryptographyProvider.Verify(dto.Password, account.Credential.PasswordHash, account.Credential.PasswordSalt))
        {
            return ApiResponse<TokenResponseDto>.Failure(invalidMessage);
        }

        // 2. Account Type Restriction
        if (account.Type != AccountType.Personal && account.Type != AccountType.System) // || account.Type == AccountType.System
        {
            return ApiResponse<TokenResponseDto>.Failure("Direct login is only supported for Personal accounts.");
        }

        // 3. Status Logic Check
        if (account.Status == AccountStatus.Banned)
        {
            return ApiResponse<TokenResponseDto>.Failure("This account has been permanently banned and locked.");
        }
        
        // Note: Suspended and PendingDeletion accounts ARE allowed to login to perform 
        // self-management or recovery (Restore) actions.

        return await GenerateTokenResponseAsync(account, deviceInfo);
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var tokenEntry = await _context.RefreshTokens
            .Include(t => t.Account)
            .ThenInclude(a => a.Credential)
            .FirstOrDefaultAsync(t => t.Token == dto.RefreshToken);

        if (tokenEntry == null || tokenEntry.IsExpired)
        {
            return ApiResponse<TokenResponseDto>.Failure("Invalid or expired refresh token.");
        }

        // Re-validate status on refresh to immediately block banned users
        if (tokenEntry.Account.Status == AccountStatus.Banned)
        {
            return ApiResponse<TokenResponseDto>.Failure("Account access has been revoked.");
        }

        _context.RefreshTokens.Remove(tokenEntry);
        
        return await GenerateTokenResponseAsync(tokenEntry.Account, tokenEntry.DeviceInfo);
    }

    public async Task<ApiResponse<MessageResponse>> LogoutAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null)
        {
            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
        return ApiResponse<MessageResponse>.Success(new MessageResponse("Logged out successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> LogoutAllDevicesAsync(Guid accountId)
    {
        var account = await _context.Accounts
            .Include(a => a.Credential)
            .Include(a => a.RefreshTokens)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null) return ApiResponse<MessageResponse>.Failure("Account not found.");

        // 1. Invalidate all Refresh Tokens
        _context.RefreshTokens.RemoveRange(account.RefreshTokens);

        // 2. Update SecurityStamp to invalidate existing Access Tokens (JWTs)
        if (account.Credential != null)
        {
            account.Credential.SecurityStamp = Guid.NewGuid().ToString("N");
            account.Credential.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return ApiResponse<MessageResponse>.Success(new MessageResponse("Successfully logged out from all devices."));
    }

    private async Task<ApiResponse<TokenResponseDto>> GenerateTokenResponseAsync(Account account, string? deviceInfo)
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
        
        // Update last login
        account.LastLogin = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return ApiResponse<TokenResponseDto>.Success(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration()
        });
    }
}
