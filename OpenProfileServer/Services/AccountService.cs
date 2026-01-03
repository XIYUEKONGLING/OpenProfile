using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;
    private readonly ISocialService _socialService;

    public AccountService(
        ApplicationDbContext context, 
        IFusionCache cache, 
        IAuthService authService,
        IVerificationService verificationService,
        ISocialService socialService)
    {
        _context = context;
        _cache = cache;
        _authService = authService;
        _verificationService = verificationService;
        _socialService = socialService;
    }

    public async Task<ApiResponse<AccountDto>> GetMyAccountAsync(Guid accountId)
    {
        var account = await _context.Accounts
            .Include(a => a.Emails)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null) return ApiResponse<AccountDto>.Failure("Account not found.");

        var primaryEmail = account.Emails.FirstOrDefault(e => e.IsPrimary)?.Email ?? string.Empty;

        return ApiResponse<AccountDto>.Success(new AccountDto
        {
            Id = account.Id,
            AccountName = account.AccountName,
            Email = primaryEmail,
            Type = account.Type,
            Role = account.Role,
            Status = account.Status,
            CreatedAt = account.CreatedAt
        });
    }

    public async Task<ApiResponse<AccountPermissionsDto>> GetMyPermissionsAsync(Guid accountId)
    {
        var cacheKey = CacheKeys.AccountPermissions(accountId);
        
        var permissions = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            var account = await _context.Accounts
                .AsNoTracking()
                .Select(a => new { a.Id, a.AccountName, a.Type, a.Role })
                .FirstOrDefaultAsync(a => a.Id == accountId);
            
            if (account == null) return null;

            return new AccountPermissionsDto
            {
                AccountId = account.Id,
                AccountName = account.AccountName,
                Type = account.Type,
                Role = account.Role
            };
        });

        if (permissions == null) return ApiResponse<AccountPermissionsDto>.Failure("Account not found.");

        return ApiResponse<AccountPermissionsDto>.Success(permissions);
    }

    public async Task<ApiResponse<PersonalSettingsDto>> GetMySettingsAsync(Guid accountId)
    {
        var settings = await _context.PersonalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == accountId);

        if (settings == null) return ApiResponse<PersonalSettingsDto>.Failure("Settings not found.");

        return ApiResponse<PersonalSettingsDto>.Success(new PersonalSettingsDto
        {
            AllowFollowers = settings.AllowFollowers,
            ShowFollowingList = settings.ShowFollowingList,
            ShowFollowersList = settings.ShowFollowersList,
            Visibility = settings.Visibility,
            DefaultVisibility = settings.DefaultVisibility,
            ShowLocalTime = settings.ShowLocalTime
        });
    }

    // POST: Full Update (Replace all)
    public async Task<ApiResponse<MessageResponse>> UpdateMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto)
    {
        var settings = await _context.PersonalSettings.FirstOrDefaultAsync(s => s.Id == accountId);
        if (settings == null) return ApiResponse<MessageResponse>.Failure("Settings not found.");

        // Full update: Use default values if null
        settings.AllowFollowers = dto.AllowFollowers ?? true;
        settings.ShowFollowingList = dto.ShowFollowingList ?? true;
        settings.ShowFollowersList = dto.ShowFollowersList ?? true;
        settings.Visibility = dto.Visibility ?? Visibility.Public;
        settings.DefaultVisibility = dto.DefaultVisibility ?? Visibility.Public;
        settings.ShowLocalTime = dto.ShowLocalTime ?? false;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountSettings(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Settings updated successfully."));
    }

    // PATCH: Partial Update
    public async Task<ApiResponse<MessageResponse>> PatchMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto)
    {
        var settings = await _context.PersonalSettings.FirstOrDefaultAsync(s => s.Id == accountId);
        if (settings == null) return ApiResponse<MessageResponse>.Failure("Settings not found.");

        if (dto.AllowFollowers.HasValue) settings.AllowFollowers = dto.AllowFollowers.Value;
        if (dto.ShowFollowingList.HasValue) settings.ShowFollowingList = dto.ShowFollowingList.Value;
        if (dto.ShowFollowersList.HasValue) settings.ShowFollowersList = dto.ShowFollowersList.Value;
        
        if (dto.Visibility.HasValue) settings.Visibility = dto.Visibility.Value;
        if (dto.DefaultVisibility.HasValue) settings.DefaultVisibility = dto.DefaultVisibility.Value;
        if (dto.ShowLocalTime.HasValue) settings.ShowLocalTime = dto.ShowLocalTime.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountSettings(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Settings updated successfully."));
    }

    public async Task<ApiResponse<ProfileDto>> GetMyProfileAsync(Guid accountId)
    {
        var profile = await _context.PersonalProfiles
            .Include(p => p.Account)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == accountId);

        if (profile == null) return ApiResponse<ProfileDto>.Failure("Profile not found.");

        return ApiResponse<ProfileDto>.Success(new ProfileDto
        {
            Id = profile.Id,
            AccountName = profile.Account.AccountName,
            Type = profile.Account.Type,
            Status = profile.Account.Status,
            DisplayName = profile.DisplayName,
            Description = profile.Description,
            Content = profile.Content,
            Location = profile.Location,
            TimeZone = profile.TimeZone,
            Website = profile.Website,
            Pronouns = profile.Pronouns,
            JobTitle = profile.JobTitle,
            CurrentCompany = profile.CurrentCompany,
            CurrentSchool = profile.CurrentSchool,
            Birthday = profile.Birthday,
            Avatar = new AssetDto { Type = profile.Avatar.Type, Value = profile.Avatar.Value, Tag = profile.Avatar.Tag },
            Background = new AssetDto { Type = profile.Background.Type, Value = profile.Background.Value, Tag = profile.Background.Tag }
        });
    }

    // POST: Full Update
    public async Task<ApiResponse<MessageResponse>> UpdateMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto)
    {
        var profile = await _context.PersonalProfiles
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.Id == accountId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        // Full Update: Replace values, null means clear.
        // Note: DisplayName is required, so we fallback to AccountName if null to prevent DB error.
        profile.DisplayName = dto.DisplayName ?? profile.Account.AccountName;
        profile.Description = dto.Description;
        profile.Content = dto.Content;
        profile.Location = dto.Location;
        profile.TimeZone = dto.TimeZone;
        profile.Website = dto.Website;
        
        profile.Pronouns = dto.Pronouns;
        profile.JobTitle = dto.JobTitle;
        profile.CurrentCompany = dto.CurrentCompany;
        profile.CurrentSchool = dto.CurrentSchool;
        profile.Birthday = dto.Birthday;

        profile.Avatar = dto.Avatar != null 
            ? new Asset { Type = dto.Avatar.Type, Value = dto.Avatar.Value, Tag = dto.Avatar.Tag } 
            : new Asset(); // Reset to default
        
        profile.Background = dto.Background != null 
            ? new Asset { Type = dto.Background.Type, Value = dto.Background.Value, Tag = dto.Background.Tag } 
            : new Asset(); // Reset to default

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated successfully."));
    }

    // PATCH: Partial Update
    public async Task<ApiResponse<MessageResponse>> PatchMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto)
    {
        var profile = await _context.PersonalProfiles.FirstOrDefaultAsync(p => p.Id == accountId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        if (dto.DisplayName != null) profile.DisplayName = dto.DisplayName;
        if (dto.Description != null) profile.Description = dto.Description;
        if (dto.Content != null) profile.Content = dto.Content;
        if (dto.Location != null) profile.Location = dto.Location;
        if (dto.TimeZone != null) profile.TimeZone = dto.TimeZone;
        if (dto.Website != null) profile.Website = dto.Website;
        
        if (dto.Pronouns != null) profile.Pronouns = dto.Pronouns;
        if (dto.JobTitle != null) profile.JobTitle = dto.JobTitle;
        if (dto.CurrentCompany != null) profile.CurrentCompany = dto.CurrentCompany;
        if (dto.CurrentSchool != null) profile.CurrentSchool = dto.CurrentSchool;
        if (dto.Birthday != null) profile.Birthday = dto.Birthday;

        if (dto.Avatar != null)
        {
            profile.Avatar = new Asset { Type = dto.Avatar.Type, Value = dto.Avatar.Value, Tag = dto.Avatar.Tag };
        }
        
        if (dto.Background != null)
        {
            profile.Background = new Asset { Type = dto.Background.Type, Value = dto.Background.Value, Tag = dto.Background.Tag };
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> ChangePasswordAsync(Guid accountId, ChangePasswordRequestDto dto)
    {
        var credential = await _context.AccountCredentials.FirstOrDefaultAsync(c => c.AccountId == accountId);
        if (credential == null) return ApiResponse<MessageResponse>.Failure("Account credentials not found.");

        if (!CryptographyProvider.Verify(dto.OldPassword, credential.PasswordHash, credential.PasswordSalt))
        {
            return ApiResponse<MessageResponse>.Failure("Current password is incorrect.");
        }

        var (hash, salt) = CryptographyProvider.CreateHash(dto.NewPassword);
        credential.PasswordHash = hash;
        credential.PasswordSalt = salt;
        credential.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _authService.LogoutAllDevicesAsync(accountId);

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Password changed successfully. All other sessions have been revoked."));
    }

    public async Task<ApiResponse<MessageResponse>> RequestDeletionAsync(Guid accountId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Account not found.");

        if (account.Status == AccountStatus.Banned)
        {
            return ApiResponse<MessageResponse>.Failure("This account is banned and cannot be self-deleted.");
        }

        account.Status = AccountStatus.PendingDeletion;
        await _context.SaveChangesAsync();
        
        await _authService.LogoutAllDevicesAsync(accountId);
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Account marked for deletion. You can restore it by logging in within 30 days."));
    }

    public async Task<ApiResponse<MessageResponse>> RestoreAccountAsync(Guid accountId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Account not found.");

        if (account.Status != AccountStatus.PendingDeletion && account.Status != AccountStatus.Suspended)
        {
            return ApiResponse<MessageResponse>.Failure("Account is not in a state that requires restoration.");
        }
        
        account.Status = AccountStatus.Active;
        await _context.SaveChangesAsync();
        
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Account restored successfully."));
    }

    // ==========================================
    // Email Management
    // ==========================================

    public async Task<ApiResponse<IEnumerable<AccountEmailDto>>> GetEmailsAsync(Guid accountId)
    {
        var emails = await _context.AccountEmails
            .AsNoTracking()
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.IsPrimary)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new AccountEmailDto
            {
                Id = e.Id,
                Email = e.Email,
                IsPrimary = e.IsPrimary,
                IsVerified = e.IsVerified,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<AccountEmailDto>>.Success(emails);
    }

    public async Task<ApiResponse<MessageResponse>> AddEmailAsync(Guid accountId, AddEmailRequestDto dto)
    {
        // 1. Uniqueness check
        if (await _context.AccountEmails.AnyAsync(e => e.Email.ToLower() == dto.Email.ToLower()))
            return ApiResponse<MessageResponse>.Failure("Email is already in use.");

        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Account not found.");

        // 2. Validate Code
        var isValid = await _verificationService.ValidateCodeAsync(dto.Email, VerificationType.VerifyEmail, dto.Code);
        if (!isValid) return ApiResponse<MessageResponse>.Failure("Invalid or expired verification code.");

        // 3. Add Verified Email
        var email = new AccountEmail
        {
            AccountId = accountId,
            Email = dto.Email,
            IsPrimary = false,
            IsVerified = true, // Verified immediately
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.AccountEmails.Add(email);
        await _context.SaveChangesAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Email added successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> SetPrimaryEmailAsync(Guid accountId, string email)
    {
        var targetEmail = await _context.AccountEmails
            .FirstOrDefaultAsync(e => e.AccountId == accountId && e.Email == email);

        if (targetEmail == null) return ApiResponse<MessageResponse>.Failure("Email not found.");

        if (!targetEmail.IsVerified)
            return ApiResponse<MessageResponse>.Failure("Only verified emails can be set as primary.");

        var currentPrimary = await _context.AccountEmails
            .FirstOrDefaultAsync(e => e.AccountId == accountId && e.IsPrimary);

        if (currentPrimary != null) currentPrimary.IsPrimary = false;
        targetEmail.IsPrimary = true;

        await _context.SaveChangesAsync();
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Primary email updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteEmailAsync(Guid accountId, string email)
    {
        var targetEmail = await _context.AccountEmails
            .FirstOrDefaultAsync(e => e.AccountId == accountId && e.Email == email);

        if (targetEmail == null) return ApiResponse<MessageResponse>.Failure("Email not found.");

        if (targetEmail.IsPrimary)
            return ApiResponse<MessageResponse>.Failure("Cannot delete primary email. Set another email as primary first.");

        _context.AccountEmails.Remove(targetEmail);
        await _context.SaveChangesAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Email removed."));
    }

    public async Task<ApiResponse<MessageResponse>> VerifyEmailAsync(Guid accountId, string email, VerifyEmailRequestDto dto)
    {
        var targetEmail = await _context.AccountEmails
            .FirstOrDefaultAsync(e => e.AccountId == accountId && e.Email == email);

        if (targetEmail == null) return ApiResponse<MessageResponse>.Failure("Email not found.");

        if (targetEmail.IsVerified) return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Email already verified."));

        var isValid = await _verificationService.ValidateCodeAsync(email, VerificationType.VerifyEmail, dto.Code);
        if (!isValid) return ApiResponse<MessageResponse>.Failure("Invalid or expired verification code.");

        targetEmail.IsVerified = true;
        targetEmail.VerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Email verified successfully."));
    }

    // ==========================================
    // Block Management
    // ==========================================

    public async Task<ApiResponse<IEnumerable<BlockDto>>> GetMyBlockedUsersAsync(Guid accountId)
    {
        return await _socialService.GetBlockedUsersAsync(accountId);
    }
}
