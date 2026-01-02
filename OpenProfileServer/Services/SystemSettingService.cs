using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
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
        return bool.TryParse(value, out var result) ? result : defaultValue;
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
            // Log error in real implementation
            return null;
        }
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = key,
                Value = value,
                Description = description,
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

        // Invalidate cache
        var cacheKey = CacheKeys.SystemSetting(key);
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task SetObjectAsync<T>(string key, T value, string? description = null) where T : class
    {
        var json = JsonConvert.SerializeObject(value);
        await SetValueAsync(key, json, description);
    }
}
