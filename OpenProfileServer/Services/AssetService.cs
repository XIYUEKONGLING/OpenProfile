using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Assets;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly ISystemSettingService _settingService;

    public AssetService(
        ApplicationDbContext context,
        IFusionCache cache,
        ISystemSettingService settingService)
    {
        _context = context;
        _cache = cache;
        _settingService = settingService;
    }

    private async Task<string?> ValidateAssetAsync(AssetDto? asset)
    {
        if (asset == null) return null;

        // 10MB default for asset library
        int maxSize = await _settingService.GetIntAsync(SystemSettingKeys.MaxLibraryAssetSizeBytes, 10485760);
        var validation = AssetValidator.Validate(asset, maxSize);
        if (!validation.Valid) return validation.Error;

        return null;
    }

    private static AccountAssetDto MapAccountAssetDto(AccountAsset asset)
    {
        return new AccountAssetDto
        {
            Id = asset.Id,
            AccountId = asset.AccountId,
            Category = asset.Category,
            Notes = asset.Notes,
            Asset = new AssetDto
            {
                Type = asset.Asset.Type,
                Value = asset.Asset.Value,
                Tag = asset.Asset.Tag
            },
            Visibility = asset.Visibility,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }

    private static SystemAssetDto MapSystemAssetDto(SystemAsset asset)
    {
        return new SystemAssetDto
        {
            Id = asset.Id,
            Category = asset.Category,
            Notes = asset.Notes,
            Asset = new AssetDto
            {
                Type = asset.Asset.Type,
                Value = asset.Asset.Value,
                Tag = asset.Asset.Tag
            },
            Visibility = asset.Visibility,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }

    // ==========================================
    // Public Asset Library (No Auth)
    // ==========================================

    public async Task<ApiResponse<PublicAssetDto>> GetPublicAssetAsync(Guid uuid)
    {
        // Search in AccountAssets first (includes user and organization assets)
        var accountAsset = await _context.AccountAssets
            .Include(a => a.Account)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (accountAsset != null)
        {
            // Check visibility
            if (accountAsset.Visibility != Visibility.Public)
                return ApiResponse<PublicAssetDto>.Failure("Asset not found.");

            // Check account status
            if (accountAsset.Account.Status == AccountStatus.Suspended ||
                accountAsset.Account.Status == AccountStatus.Banned ||
                accountAsset.Account.Status == AccountStatus.PendingDeletion)
            {
                return ApiResponse<PublicAssetDto>.Failure("Asset not found.");
            }

            return ApiResponse<PublicAssetDto>.Success(new PublicAssetDto
            {
                Id = accountAsset.Id,
                AccountId = accountAsset.AccountId,
                Category = accountAsset.Category,
                Notes = accountAsset.Notes,
                Asset = new AssetDto
                {
                    Type = accountAsset.Asset.Type,
                    Value = accountAsset.Asset.Value,
                    Tag = accountAsset.Asset.Tag
                },
                Visibility = accountAsset.Visibility,
                CreatedAt = accountAsset.CreatedAt,
                UpdatedAt = accountAsset.UpdatedAt
            });
        }

        // Search in SystemAssets
        var systemAsset = await _context.SystemAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (systemAsset != null)
        {
            // Check visibility
            if (systemAsset.Visibility != Visibility.Public)
                return ApiResponse<PublicAssetDto>.Failure("Asset not found.");

            return ApiResponse<PublicAssetDto>.Success(new PublicAssetDto
            {
                Id = systemAsset.Id,
                AccountId = null,  // System assets have no owner
                Category = systemAsset.Category,
                Notes = systemAsset.Notes,
                Asset = new AssetDto
                {
                    Type = systemAsset.Asset.Type,
                    Value = systemAsset.Asset.Value,
                    Tag = systemAsset.Asset.Tag
                },
                Visibility = systemAsset.Visibility,
                CreatedAt = systemAsset.CreatedAt,
                UpdatedAt = systemAsset.UpdatedAt
            });
        }

        return ApiResponse<PublicAssetDto>.Failure("Asset not found.");
    }

    // ==========================================
    // Cross-Library Asset Lookup
    // ==========================================

    public async Task<ApiResponse<LookupAssetDto>> LookupAssetAsync(Guid uuid, Guid currentUserId)
    {
        // Search in AccountAssets (user and organization assets)
        var accountAsset = await _context.AccountAssets
            .Include(a => a.Account)
                .ThenInclude(ac => ac.Memberships)
            .Include(a => a.Account)
                .ThenInclude(ac => ac.Followers)
            .Include(a => a.Account)
                .ThenInclude(ac => ac.Following)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (accountAsset != null)
        {
            var account = accountAsset.Account;
            var assetOwnerId = account.Id;

            // Check account status
            if (account.Status == AccountStatus.Suspended ||
                account.Status == AccountStatus.Banned ||
                account.Status == AccountStatus.PendingDeletion)
            {
                return ApiResponse<LookupAssetDto>.Failure("Asset not found.");
            }

            var isOwner = currentUserId == assetOwnerId;

            // Check visibility based on current user
            bool hasAccess = accountAsset.Visibility switch
            {
                Visibility.Public => true,
                Visibility.Authenticated => true,  // Already authenticated
                Visibility.Private => isOwner,
                Visibility.Protected => true,  // Already authenticated
                Visibility.FriendsOnly when isOwner => true,
                Visibility.FriendsOnly => CheckIsFriend(account, currentUserId),
                Visibility.MembersOnly when isOwner => true,
                Visibility.MembersOnly when account.Type == AccountType.Organization =>
                    CheckIsOrganizationMember(account, currentUserId),
                Visibility.MembersOnly => false,  // Personal account with Membership visibility - not supported
                _ => false
            };

            if (!hasAccess)
                return ApiResponse<LookupAssetDto>.Failure("Asset not found.");

            return ApiResponse<LookupAssetDto>.Success(new LookupAssetDto
            {
                Id = accountAsset.Id,
                AccountId = accountAsset.AccountId,
                Category = accountAsset.Category,
                Notes = accountAsset.Notes,
                Asset = new AssetDto
                {
                    Type = accountAsset.Asset.Type,
                    Value = accountAsset.Asset.Value,
                    Tag = accountAsset.Asset.Tag
                },
                Visibility = accountAsset.Visibility,
                CreatedAt = accountAsset.CreatedAt,
                UpdatedAt = accountAsset.UpdatedAt
            });
        }

        // Search in SystemAssets
        var systemAsset = await _context.SystemAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (systemAsset != null)
        {
            // Get current user role
            var currentUser = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == currentUserId);

            bool isAdmin = currentUser != null &&
                           (currentUser.Role == AccountRole.Admin || currentUser.Role == AccountRole.Root);

            // Check visibility
            bool hasAccess = systemAsset.Visibility switch
            {
                Visibility.Public => true,
                Visibility.Authenticated => true,  // Already authenticated
                Visibility.Protected => true,  // Already authenticated
                Visibility.Private => isAdmin,  // Only admins can see private system assets
                Visibility.FriendsOnly => false,  // Not applicable to system assets
                Visibility.MembersOnly => false,  // Not applicable to system assets
                _ => false
            };

            if (!hasAccess)
                return ApiResponse<LookupAssetDto>.Failure("Asset not found.");

            return ApiResponse<LookupAssetDto>.Success(new LookupAssetDto
            {
                Id = systemAsset.Id,
                AccountId = null,  // System assets have no owner
                Category = systemAsset.Category,
                Notes = systemAsset.Notes,
                Asset = new AssetDto
                {
                    Type = systemAsset.Asset.Type,
                    Value = systemAsset.Asset.Value,
                    Tag = systemAsset.Asset.Tag
                },
                Visibility = systemAsset.Visibility,
                CreatedAt = systemAsset.CreatedAt,
                UpdatedAt = systemAsset.UpdatedAt
            });
        }

        return ApiResponse<LookupAssetDto>.Failure("Asset not found.");
    }

    private static bool CheckIsFriend(Account account, Guid currentUserId)
    {
        return account.Followers.Any(f => f.FollowerId == currentUserId) &&
               account.Following.Any(f => f.FollowingId == currentUserId);
    }

    private static bool CheckIsOrganizationMember(Account account, Guid currentUserId)
    {
        return account.Memberships.Any(m => m.AccountId == currentUserId);
    }

    // ==========================================
    // Personal Asset Management
    // ==========================================

    public async Task<ApiResponse<PagedResponse<AccountAssetDto>>> GetMyAssetsAsync(
        Guid accountId,
        int page,
        int pageSize,
        string? category,
        Visibility? visibility,
        string? search)
    {
        var query = _context.AccountAssets
            .AsNoTracking()
            .Where(a => a.AccountId == accountId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (visibility.HasValue)
            query = query.Where(a => a.Visibility == visibility.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => (a.Category != null && a.Category.Contains(search)) ||
                                    (a.Notes != null && a.Notes.Contains(search)) ||
                                    (a.Asset.Tag != null && a.Asset.Tag.Contains(search)));

        var totalCount = await query.CountAsync();

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapAccountAssetDto(a))
            .ToListAsync();

        return ApiResponse<PagedResponse<AccountAssetDto>>.Success(new PagedResponse<AccountAssetDto>(
            assets,
            page,
            pageSize,
            totalCount
        ));
    }

    public async Task<ApiResponse<AccountAssetDto>> GetMyAssetAsync(Guid accountId, Guid uuid)
    {
        var asset = await _context.AccountAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == accountId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> CreateMyAssetAsync(Guid accountId, CreateAccountAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<AccountAssetDto>.Failure(validationError);

        var asset = new AccountAsset
        {
            AccountId = accountId,
            Category = dto.Category,
            Notes = dto.Notes,
            Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            },
            Visibility = dto.Visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AccountAssets.Add(asset);
        await _context.SaveChangesAsync();

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> UpdateMyAssetAsync(Guid accountId, Guid uuid, CreateAccountAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<AccountAssetDto>.Failure(validationError);

        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == accountId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Full update
        asset.Category = dto.Category;
        asset.Notes = dto.Notes;
        asset.Asset = new Asset
        {
            Type = dto.Asset.Type,
            Value = dto.Asset.Value,
            Tag = dto.Asset.Tag
        };
        asset.Visibility = dto.Visibility;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> PatchMyAssetAsync(Guid accountId, Guid uuid, UpdateAccountAssetRequestDto dto)
    {
        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == accountId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Partial update
        if (dto.Category != null)
            asset.Category = dto.Category;

        if (dto.Notes != null)
            asset.Notes = dto.Notes;

        if (dto.Asset != null)
        {
            var validationError = await ValidateAssetAsync(dto.Asset);
            if (validationError != null)
                return ApiResponse<AccountAssetDto>.Failure(validationError);

            asset.Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            };
        }

        if (dto.Visibility.HasValue)
            asset.Visibility = dto.Visibility.Value;

        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteMyAssetAsync(Guid accountId, Guid uuid)
    {
        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == accountId);

        if (asset == null)
            return ApiResponse<MessageResponse>.Failure("Asset not found.");

        _context.AccountAssets.Remove(asset);
        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Asset deleted successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> BatchUpdateMyAssetVisibilityAsync(Guid accountId, BatchUpdateVisibilityRequestDto dto)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            return ApiResponse<MessageResponse>.Failure("No assets specified.");

        var assets = await _context.AccountAssets
            .Where(a => a.AccountId == accountId && dto.AssetIds.Contains(a.Id))
            .ToListAsync();

        foreach (var asset in assets)
        {
            asset.Visibility = dto.Visibility;
            asset.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate cache for all affected assets
        foreach (var assetId in dto.AssetIds)
        {
            await InvalidateAssetCacheAsync(assetId);
        }

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"Updated {assets.Count} asset(s)."));
    }

    public async Task<ApiResponse<MessageResponse>> BatchDeleteMyAssetsAsync(Guid accountId, BatchDeleteRequestDto dto)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            return ApiResponse<MessageResponse>.Failure("No assets specified.");

        var assets = await _context.AccountAssets
            .Where(a => a.AccountId == accountId && dto.AssetIds.Contains(a.Id))
            .ToListAsync();

        _context.AccountAssets.RemoveRange(assets);
        await _context.SaveChangesAsync();

        // Invalidate cache for all affected assets
        foreach (var assetId in dto.AssetIds)
        {
            await InvalidateAssetCacheAsync(assetId);
        }

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"Deleted {assets.Count} asset(s)."));
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetMyAssetCategoriesAsync(Guid accountId)
    {
        var categories = await _context.AccountAssets
            .AsNoTracking()
            .Where(a => a.AccountId == accountId && a.Category != null)
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return ApiResponse<IEnumerable<string>>.Success(categories);
    }

    // ==========================================
    // Organization Asset Management
    // ==========================================

    public async Task<ApiResponse<PagedResponse<AccountAssetDto>>> GetOrganizationAssetsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? category,
        Visibility? visibility,
        string? search)
    {
        var query = _context.AccountAssets
            .AsNoTracking()
            .Where(a => a.AccountId == organizationId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (visibility.HasValue)
            query = query.Where(a => a.Visibility == visibility.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => (a.Category != null && a.Category.Contains(search)) ||
                                    (a.Notes != null && a.Notes.Contains(search)) ||
                                    (a.Asset.Tag != null && a.Asset.Tag.Contains(search)));

        var totalCount = await query.CountAsync();

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapAccountAssetDto(a))
            .ToListAsync();

        return ApiResponse<PagedResponse<AccountAssetDto>>.Success(new PagedResponse<AccountAssetDto>(
            assets,
            page,
            pageSize,
            totalCount
        ));
    }

    public async Task<ApiResponse<AccountAssetDto>> GetOrganizationAssetAsync(Guid organizationId, Guid uuid)
    {
        var asset = await _context.AccountAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == organizationId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> CreateOrganizationAssetAsync(Guid organizationId, CreateAccountAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<AccountAssetDto>.Failure(validationError);

        var asset = new AccountAsset
        {
            AccountId = organizationId,
            Category = dto.Category,
            Notes = dto.Notes,
            Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            },
            Visibility = dto.Visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AccountAssets.Add(asset);
        await _context.SaveChangesAsync();

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> UpdateOrganizationAssetAsync(Guid organizationId, Guid uuid, CreateAccountAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<AccountAssetDto>.Failure(validationError);

        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == organizationId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Full update
        asset.Category = dto.Category;
        asset.Notes = dto.Notes;
        asset.Asset = new Asset
        {
            Type = dto.Asset.Type,
            Value = dto.Asset.Value,
            Tag = dto.Asset.Tag
        };
        asset.Visibility = dto.Visibility;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<AccountAssetDto>> PatchOrganizationAssetAsync(Guid organizationId, Guid uuid, UpdateAccountAssetRequestDto dto)
    {
        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == organizationId);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Partial update
        if (dto.Category != null)
            asset.Category = dto.Category;

        if (dto.Notes != null)
            asset.Notes = dto.Notes;

        if (dto.Asset != null)
        {
            var validationError = await ValidateAssetAsync(dto.Asset);
            if (validationError != null)
                return ApiResponse<AccountAssetDto>.Failure(validationError);

            asset.Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            };
        }

        if (dto.Visibility.HasValue)
            asset.Visibility = dto.Visibility.Value;

        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteOrganizationAssetAsync(Guid organizationId, Guid uuid)
    {
        var asset = await _context.AccountAssets
            .FirstOrDefaultAsync(a => a.Id == uuid && a.AccountId == organizationId);

        if (asset == null)
            return ApiResponse<MessageResponse>.Failure("Asset not found.");

        _context.AccountAssets.Remove(asset);
        await _context.SaveChangesAsync();
        await InvalidateAssetCacheAsync(uuid);

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Asset deleted successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> BatchUpdateOrganizationAssetVisibilityAsync(Guid organizationId, BatchUpdateVisibilityRequestDto dto)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            return ApiResponse<MessageResponse>.Failure("No assets specified.");

        var assets = await _context.AccountAssets
            .Where(a => a.AccountId == organizationId && dto.AssetIds.Contains(a.Id))
            .ToListAsync();

        foreach (var asset in assets)
        {
            asset.Visibility = dto.Visibility;
            asset.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate cache for all affected assets
        foreach (var assetId in dto.AssetIds)
        {
            await InvalidateAssetCacheAsync(assetId);
        }

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"Updated {assets.Count} asset(s)."));
    }

    public async Task<ApiResponse<MessageResponse>> BatchDeleteOrganizationAssetsAsync(Guid organizationId, BatchDeleteRequestDto dto)
    {
        if (dto.AssetIds == null || dto.AssetIds.Count == 0)
            return ApiResponse<MessageResponse>.Failure("No assets specified.");

        var assets = await _context.AccountAssets
            .Where(a => a.AccountId == organizationId && dto.AssetIds.Contains(a.Id))
            .ToListAsync();

        _context.AccountAssets.RemoveRange(assets);
        await _context.SaveChangesAsync();

        // Invalidate cache for all affected assets
        foreach (var assetId in dto.AssetIds)
        {
            await InvalidateAssetCacheAsync(assetId);
        }

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"Deleted {assets.Count} asset(s)."));
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetOrganizationAssetCategoriesAsync(Guid organizationId)
    {
        var categories = await _context.AccountAssets
            .AsNoTracking()
            .Where(a => a.AccountId == organizationId && a.Category != null)
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return ApiResponse<IEnumerable<string>>.Success(categories);
    }

    // ==========================================
    // System Asset Management (Admin Only)
    // ==========================================

    public async Task<ApiResponse<PagedResponse<SystemAssetDto>>> GetSystemAssetsAsync(
        int page,
        int pageSize,
        string? category,
        Visibility? visibility,
        string? search)
    {
        var query = _context.SystemAssets.AsNoTracking();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (visibility.HasValue)
            query = query.Where(a => a.Visibility == visibility.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => (a.Category != null && a.Category.Contains(search)) ||
                                    (a.Notes != null && a.Notes.Contains(search)) ||
                                    (a.Asset.Tag != null && a.Asset.Tag.Contains(search)));

        var totalCount = await query.CountAsync();

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapSystemAssetDto(a))
            .ToListAsync();

        return ApiResponse<PagedResponse<SystemAssetDto>>.Success(new PagedResponse<SystemAssetDto>(
            assets,
            page,
            pageSize,
            totalCount
        ));
    }

    public async Task<ApiResponse<SystemAssetDto>> GetSystemAssetAsync(Guid uuid)
    {
        var asset = await _context.SystemAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (asset == null)
            return ApiResponse<SystemAssetDto>.Failure("Asset not found.");

        return ApiResponse<SystemAssetDto>.Success(MapSystemAssetDto(asset));
    }

    public async Task<ApiResponse<SystemAssetDto>> CreateSystemAssetAsync(CreateSystemAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<SystemAssetDto>.Failure(validationError);

        var asset = new SystemAsset
        {
            Category = dto.Category,
            Notes = dto.Notes,
            Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            },
            Visibility = dto.Visibility,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SystemAssets.Add(asset);
        await _context.SaveChangesAsync();

        return ApiResponse<SystemAssetDto>.Success(MapSystemAssetDto(asset));
    }

    public async Task<ApiResponse<SystemAssetDto>> UpdateSystemAssetAsync(Guid uuid, CreateSystemAssetRequestDto dto)
    {
        var validationError = await ValidateAssetAsync(dto.Asset);
        if (validationError != null)
            return ApiResponse<SystemAssetDto>.Failure(validationError);

        var asset = await _context.SystemAssets
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (asset == null)
            return ApiResponse<SystemAssetDto>.Failure("Asset not found.");

        // Full update
        asset.Category = dto.Category;
        asset.Notes = dto.Notes;
        asset.Asset = new Asset
        {
            Type = dto.Asset.Type,
            Value = dto.Asset.Value,
            Tag = dto.Asset.Tag
        };
        asset.Visibility = dto.Visibility;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateSystemAssetCacheAsync(uuid);

        return ApiResponse<SystemAssetDto>.Success(MapSystemAssetDto(asset));
    }

    public async Task<ApiResponse<SystemAssetDto>> PatchSystemAssetAsync(Guid uuid, UpdateSystemAssetRequestDto dto)
    {
        var asset = await _context.SystemAssets
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (asset == null)
            return ApiResponse<SystemAssetDto>.Failure("Asset not found.");

        // Partial update
        if (dto.Category != null)
            asset.Category = dto.Category;

        if (dto.Notes != null)
            asset.Notes = dto.Notes;

        if (dto.Asset != null)
        {
            var validationError = await ValidateAssetAsync(dto.Asset);
            if (validationError != null)
                return ApiResponse<SystemAssetDto>.Failure(validationError);

            asset.Asset = new Asset
            {
                Type = dto.Asset.Type,
                Value = dto.Asset.Value,
                Tag = dto.Asset.Tag
            };
        }

        if (dto.Visibility.HasValue)
            asset.Visibility = dto.Visibility.Value;

        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await InvalidateSystemAssetCacheAsync(uuid);

        return ApiResponse<SystemAssetDto>.Success(MapSystemAssetDto(asset));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteSystemAssetAsync(Guid uuid)
    {
        var asset = await _context.SystemAssets
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (asset == null)
            return ApiResponse<MessageResponse>.Failure("Asset not found.");

        _context.SystemAssets.Remove(asset);
        await _context.SaveChangesAsync();
        await InvalidateSystemAssetCacheAsync(uuid);

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Asset deleted successfully."));
    }

    // ==========================================
    // Cache Helpers
    // ==========================================

    private ValueTask InvalidateAssetCacheAsync(Guid uuid)
    {
        return _cache.RemoveAsync(CacheKeys.PublicAsset(uuid));
    }

    private ValueTask InvalidateSystemAssetCacheAsync(Guid uuid)
    {
        return _cache.RemoveAsync(CacheKeys.SystemAsset(uuid));
    }
}
