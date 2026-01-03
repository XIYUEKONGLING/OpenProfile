using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Auth;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Utilities;

namespace OpenProfileServer.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(ApplicationDbContext context, ITokenService tokenService, IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
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
        if (account.Type == AccountType.Organization || account.Type == AccountType.Application) // || account.Type == AccountType.System
        {
            return ApiResponse<TokenResponseDto>.Failure("Direct login is not supported for this account type.");
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
