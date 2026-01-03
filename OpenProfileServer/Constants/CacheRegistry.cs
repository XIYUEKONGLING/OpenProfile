namespace OpenProfileServer.Constants;

public static class CacheKeys
{
    // ==========================================
    // Site & System Wide
    // ==========================================
    
    public const string SiteMetadata = "Site:Metadata";

    public static string SystemSetting(string key) => $"System:Setting:{key}";
    
    // ==========================================
    // Accounts & Profiles
    // ==========================================

    /// <summary>
    /// Maps an AccountName (username) to an AccountId (GUID).
    /// </summary>
    public static string AccountNameMapping(string name) => $"Account:Mapping:{name.ToLowerInvariant()}";

    public static string AccountProfile(Guid accountId) => $"Account:Profile:{accountId}";

    public static string AccountSettings(Guid accountId) => $"Account:Settings:{accountId}";

    public static string AccountPermissions(Guid accountId) => $"Account:Permissions:{accountId}";

    // ==========================================
    // Social
    // ==========================================

    /// <summary>
    /// Cache key for checking if User A follows User B.
    /// Format: Social:Follow:{FollowerId}:{FollowingId}
    /// </summary>
    public static string SocialFollow(Guid followerId, Guid followingId) => $"Social:Follow:{followerId}:{followingId}";
    
    /// <summary>
    /// Cache key for checking if User A blocks User B.
    /// </summary>
    public static string SocialBlock(Guid blockerId, Guid blockedId) => $"Social:Block:{blockerId}:{blockedId}";
}