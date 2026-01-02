namespace OpenProfileServer.Interfaces;

public interface ISystemSettingService
{
    Task<string?> GetValueAsync(string key);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task SetValueAsync(string key, string value, string? description = null);
}