using OpenProfileServer.Models.DTOs.Admin;

namespace OpenProfileServer.Interfaces;

public interface ISystemSettingService
{
    Task<string?> GetValueAsync(string key);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    
    /// <summary>
    /// Gets a setting value and deserializes it into the specified type.
    /// </summary>
    Task<T?> GetObjectAsync<T>(string key) where T : class;
    
    Task SetValueAsync(string key, string value, string? description = null);
    
    /// <summary>
    /// Serializes an object to JSON and saves it as a setting value.
    /// </summary>
    Task SetObjectAsync<T>(string key, T value, string? description = null) where T : class;
    
    Task<IEnumerable<SystemSettingDto>> GetAllSettingsAsync();
}