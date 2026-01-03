using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.Enums;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;

    public ProfileService(ApplicationDbContext context, IFusionCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid?> ResolveIdAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;

        // 1. Direct UUID check (@guid)
        if (identifier.StartsWith('@') && Guid.TryParse(identifier.AsSpan(1), out var guid))
        {
            return guid;
        }

        // 2. Resolve Username -> UUID (Cached)
        var cacheKey = CacheKeys.AccountNameMapping(identifier);
        
        return await _cache.GetOrSetAsync(
            cacheKey,
            async _ =>
            {
                var account = await _context.Accounts
                    .AsNoTracking()
                    .Where(a => a.AccountName == identifier)
                    .Select(a => new { a.Id })
                    .FirstOrDefaultAsync();
                
                return account?.Id;
            },
            tags: [cacheKey]
        );
    }

    public async Task<ApiResponse<ProfileDto>> GetProfileAsync(string identifier)
    {
        var accountId = await ResolveIdAsync(identifier);
        if (accountId == null)
        {
            return ApiResponse<ProfileDto>.Failure("Profile not found.");
        }

        var id = accountId.Value;
        var cacheKey = CacheKeys.AccountProfile(id);

        var profileDto = await _cache.GetOrSetAsync(
            cacheKey,
            async _ => await FetchProfileFromDbAsync(id),
            tags: [cacheKey]
        );

        if (profileDto == null)
        {
            return ApiResponse<ProfileDto>.Failure("Profile not found.");
        }

        // Status Logic: If Banned, we only show minimal info (handled in mapping or here).
        // The Spec says: Banned -> Banned Status Only. 
        if (profileDto.Status == AccountStatus.Banned)
        {
            return ApiResponse<ProfileDto>.Success(new ProfileDto
            {
                Id = profileDto.Id,
                AccountName = profileDto.AccountName,
                Type = profileDto.Type,
                Status = AccountStatus.Banned,
                DisplayName = "Account Banned",
                Description = "This account has been suspended for violating our terms of service."
            });
        }
        
        // Note: Suspended accounts show restricted info, but the full DTO might be cached.
        // We filter it at runtime here if needed, or rely on the frontend to respect the Status field.
        // For security, if Suspended, we mask sensitive fields.
        if (profileDto.Status == AccountStatus.Suspended)
        {
             // Mask detailed content for suspended users
             profileDto.Content = null;
             profileDto.Website = null;
             profileDto.Location = null;
        }

        return ApiResponse<ProfileDto>.Success(profileDto);
    }

    private async Task<ProfileDto?> FetchProfileFromDbAsync(Guid id)
    {
        // Polymorphic query to get base account and the specific profile
        var account = await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Profile) // Base profile
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null || account.Profile == null) return null;

        // Base DTO
        var dto = new ProfileDto
        {
            Id = account.Id,
            AccountName = account.AccountName,
            Type = account.Type,
            Status = account.Status,
            DisplayName = account.Profile.DisplayName,
            Description = account.Profile.Description,
            Content = account.Profile.Content,
            Location = account.Profile.Location,
            TimeZone = account.Profile.TimeZone,
            Website = account.Profile.Website,
            Avatar = new AssetDto 
            { 
                Type = account.Profile.Avatar.Type, 
                Value = account.Profile.Avatar.Value, 
                Tag = account.Profile.Avatar.Tag 
            },
            Background = new AssetDto
            {
                Type = account.Profile.Background.Type, 
                Value = account.Profile.Background.Value, 
                Tag = account.Profile.Background.Tag 
            }
        };

        // Specific mappings based on Type
        if (account.Type == AccountType.Personal)
        {
            // We need to cast or query the specific table if we need specific fields not in base.
            // Since EF Core loaded it via .Include(a => a.Profile), the runtime type matches.
            if (account.Profile is Models.Entities.Profiles.PersonalProfile pp)
            {
                dto.Pronouns = pp.Pronouns;
                dto.JobTitle = pp.JobTitle;
                dto.CurrentCompany = pp.CurrentCompany;
                dto.CurrentSchool = pp.CurrentSchool;
                dto.Birthday = pp.Birthday;
            }
        }
        else if (account.Type == AccountType.Organization)
        {
            if (account.Profile is Models.Entities.Profiles.OrganizationProfile op)
            {
                dto.FoundedDate = op.FoundedDate;
            }
        }

        // Statistics (Count) - Usually cached separately or updated periodically.
        // For simplicity, we count here, but in high-load, this should be denormalized.
        dto.FollowersCount = await _context.AccountFollowers.CountAsync(f => f.FollowingId == id);
        dto.FollowingCount = await _context.AccountFollowers.CountAsync(f => f.FollowerId == id);

        return dto;
    }
}
