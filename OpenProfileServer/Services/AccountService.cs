using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
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

    public AccountService(
        ApplicationDbContext context, 
        IFusionCache cache, 
        IAuthService authService)
    {
        _context = context;
        _cache = cache;
        _authService = authService;
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

    public async Task<ApiResponse<MessageResponse>> UpdateMySettingsAsync(Guid accountId, UpdatePersonalSettingsRequestDto dto)
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
        
        // Invalidate settings cache
        await _cache.RemoveAsync(CacheKeys.AccountSettings(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Settings updated successfully."));
    }

    public async Task<ApiResponse<ProfileDto>> GetMyProfileAsync(Guid accountId)
    {
        // For "Edit Mode", we shouldn't rely heavily on the public cache, 
        // as we want to see the latest version immediately.
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

    public async Task<ApiResponse<MessageResponse>> UpdateMyProfileAsync(Guid accountId, UpdateProfileRequestDto dto)
    {
        var profile = await _context.PersonalProfiles.FirstOrDefaultAsync(p => p.Id == accountId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        // Map updates
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
        
        // Invalidate public profile cache
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> ChangePasswordAsync(Guid accountId, ChangePasswordRequestDto dto)
    {
        var credential = await _context.AccountCredentials.FirstOrDefaultAsync(c => c.AccountId == accountId);
        if (credential == null) return ApiResponse<MessageResponse>.Failure("Account credentials not found.");

        // Verify old
        if (!CryptographyProvider.Verify(dto.OldPassword, credential.PasswordHash, credential.PasswordSalt))
        {
            return ApiResponse<MessageResponse>.Failure("Current password is incorrect.");
        }

        // Set new
        var (hash, salt) = CryptographyProvider.CreateHash(dto.NewPassword);
        credential.PasswordHash = hash;
        credential.PasswordSalt = salt;
        credential.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Optional: Logout other devices for security
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

        // Set to PendingDeletion (Cooling-off period)
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
        
        // Restore to Active
        account.Status = AccountStatus.Active;
        await _context.SaveChangesAsync();
        
        await _cache.RemoveAsync(CacheKeys.AccountProfile(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Account restored successfully."));
    }
}
