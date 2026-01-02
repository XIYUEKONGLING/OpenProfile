namespace OpenProfileServer.Constants;

public static class CacheKeys
{
    // ==========================================
    // Site & System Wide
    // ==========================================
    
    /// <summary>
    /// Cache key for the singleton site metadata record.
    /// </summary>
    public const string SiteMetadata = "Site:Metadata";

    /// <summary>
    /// Generates a cache key for a specific system setting.
    /// </summary>
    public static string SystemSetting(string key) => $"System:Setting:{key}";
    
    // ==========================================
    // Accounts & Profiles
    // ==========================================

    /// <summary>
    /// Generates a cache key for an account's public profile.
    /// </summary>
    public static string AccountProfile(Guid id) => $"Account:Profile:{id}";
}