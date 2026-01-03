namespace OpenProfileServer.Constants;

public static class CacheKeys
{
    // ==========================================
    // Site & System Wide
    // ==========================================
    
    public const string SiteMetadata = "Site:Metadata";
    
    public const string SystemSettingsList = "System:Settings:List";

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

    // ==========================================
    // Profile Sub-Resources
    // ==========================================

    public static string ProfileWork(Guid profileId) => $"Profile:Work:{profileId}";
    public static string ProfileEducation(Guid profileId) => $"Profile:Education:{profileId}";
    public static string ProfileProjects(Guid profileId) => $"Profile:Projects:{profileId}";
    public static string ProfileSocials(Guid profileId) => $"Profile:Socials:{profileId}";
    
    public static string ProfileCertificates(Guid profileId) => $"Profile:Certificates:{profileId}";
    public static string ProfileSponsorships(Guid profileId) => $"Profile:Sponsorships:{profileId}";
    public static string ProfileGallery(Guid profileId) => $"Profile:Gallery:{profileId}";
    
    public static string ProfileMemberships(Guid profileId) => $"Profile:Memberships:{profileId}";
    
    // ==========================================
    // Organizations
    // ==========================================
    
    /// <summary>
    /// Cache key for an organization's member list (public view).
    /// </summary>
    public static string OrganizationMembers(Guid orgId) => $"Org:Members:{orgId}";
    
    /// <summary>
    /// Cache key for a user's membership list (which orgs do I belong to?).
    /// </summary>
    public static string UserMemberships(Guid userId) => $"User:Memberships:{userId}";
    
    
    /// <summary>
    /// Cache key to quickly check a user's role in an org.
    /// </summary>
    public static string MemberRole(Guid orgId, Guid userId) => $"Org:Role:{orgId}:{userId}";

}
