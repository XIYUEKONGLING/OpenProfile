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

    public async Task<ApiResponse<AccountAssetDto>> GetPublicAssetAsync(Guid uuid)
    {
        var asset = await _context.AccountAssets
            .Include(a => a.Account)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == uuid);

        if (asset == null)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Check visibility
        if (asset.Visibility != Visibility.Public)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        // Check account status
        if (asset.Account.Status == AccountStatus.Suspended || asset.Account.Status == AccountStatus.Banned)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        if (asset.Account.Status == AccountStatus.PendingDeletion)
            return ApiResponse<AccountAssetDto>.Failure("Asset not found.");

        return ApiResponse<AccountAssetDto>.Success(MapAccountAssetDto(asset));
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
    // System Asset Management (Admin Only)
    // ==========================================

    public async Task<ApiResponse<PagedResponse<SystemAssetDto>>> GetSystemAssetsAsync(
        int page,
        int pageSize,
        string? category,
        Visibility? visibility)
    {
        var query = _context.SystemAssets.AsNoTracking();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (visibility.HasValue)
            query = query.Where(a => a.Visibility == visibility.Value);

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
