namespace OpenProfileServer.Constants;

public static class CacheKeys
{
    public static string SystemSetting(string key) => $"System:Setting:{key}";
    
    public static string AccountProfile(Guid id) => $"Account:Profile:{id}";
}