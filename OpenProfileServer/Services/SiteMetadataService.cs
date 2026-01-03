using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.DTOs.Site;
using OpenProfileServer.Models.ValueObjects;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class SiteMetadataService : ISiteMetadataService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly ISystemSettingService _settingService;

    public SiteMetadataService(ApplicationDbContext context, IFusionCache cache, ISystemSettingService settingService)
    {
        _context = context;
        _cache = cache;
        _settingService = settingService;
    }

    private async Task<string?> ValidateMetaAssetsAsync(UpdateSiteMetadataRequestDto dto)
    {
        int limit = await _settingService.GetIntAsync(SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        
        var vLogo = AssetValidator.Validate(dto.Logo, limit);
        if (!vLogo.Valid) return vLogo.Error;

        var vFav = AssetValidator.Validate(dto.Favicon, limit);
        if (!vFav.Valid) return vFav.Error;

        return null;
    }

    public async Task<SiteMetadata> GetMetadataAsync() => 
        await _cache.GetOrSetAsync(CacheKeys.SiteMetadata, async _ => 
            await _context.SiteMetadata.FirstOrDefaultAsync() ?? new SiteMetadata());

    public async Task UpdateMetadataAsync(SiteMetadata metadata)
    {
        var existing = await _context.SiteMetadata.FirstOrDefaultAsync();
        if (existing == null)
        {
            metadata.Id = 1;
            _context.SiteMetadata.Add(metadata);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(metadata);
            
            existing.Logo = metadata.Logo;
            existing.Favicon = metadata.Favicon;
            
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.SiteMetadata);
    }

    public async Task PatchMetadataAsync(UpdateSiteMetadataRequestDto dto)
    {
        var assetError = await ValidateMetaAssetsAsync(dto);
        if (assetError != null) throw new ArgumentException(assetError);

        var existing = await _context.SiteMetadata.FirstOrDefaultAsync();
        if (existing == null)
        {
            existing = new SiteMetadata { Id = 1 };
            _context.SiteMetadata.Add(existing);
        }

        if (dto.SiteName != null) existing.SiteName = dto.SiteName;
        if (dto.SiteDescription != null) existing.SiteDescription = dto.SiteDescription;
        if (dto.Copyright != null) existing.Copyright = dto.Copyright;
        if (dto.ContactEmail != null) existing.ContactEmail = dto.ContactEmail;
        
        if (dto.Logo != null)
        {
            existing.Logo = new Asset 
            { 
                Type = dto.Logo.Type, 
                Value = dto.Logo.Value, 
                Tag = dto.Logo.Tag 
            };
        }

        if (dto.Favicon != null)
        {
            existing.Favicon = new Asset 
            { 
                Type = dto.Favicon.Type, 
                Value = dto.Favicon.Value, 
                Tag = dto.Favicon.Tag 
            };
        }

        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        await _cache.RemoveAsync(CacheKeys.SiteMetadata);
    }
}
