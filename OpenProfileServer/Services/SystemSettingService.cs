using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Admin;
using OpenProfileServer.Models.Entities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;

    public SystemSettingService(ApplicationDbContext context, IFusionCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var cacheKey = CacheKeys.SystemSetting(key);

        return await _cache.GetOrSetAsync(
            cacheKey,
            async _ =>
            {
                var setting = await _context.SystemSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == key);
                return setting?.Value;
            },
            tags: [cacheKey]
        );
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        
        // Handle "true"/"false" and "1"/"0"
        if (bool.TryParse(value, out var result)) return result;
        if (value == "1") return true;
        if (value == "0") return false;
        
        return defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<T?> GetObjectAsync<T>(string key) where T : class
    {
        var value = await GetValueAsync(key);
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            // Usually we expect keys to be seeded, but we allow creating new ones dynamically if needed.
            setting = new SystemSetting
            {
                Key = key,
                Value = value,
                Description = description,
                ValueType = "string", // Default
                UpdatedAt = DateTime.UtcNow
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            if (description != null) setting.Description = description;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate specific key
        await _cache.RemoveAsync(CacheKeys.SystemSetting(key));
        // Invalidate the list cache as content changed
        await _cache.RemoveAsync(CacheKeys.SystemSettingsList);
    }

    public async Task SetObjectAsync<T>(string key, T value, string? description = null) where T : class
    {
        var json = JsonConvert.SerializeObject(value);
        await SetValueAsync(key, json, description);
    }

    // [NEW] Implementation
    public async Task<IEnumerable<SystemSettingDto>> GetAllSettingsAsync()
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.SystemSettingsList,
            async _ => await _context.SystemSettings
                .AsNoTracking()
                .OrderBy(s => s.Key)
                .Select(s => new SystemSettingDto
                {
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    ValueType = s.ValueType,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync(),
            tags: [CacheKeys.SystemSettingsList]
        ) ?? new List<SystemSettingDto>();
    }
}
