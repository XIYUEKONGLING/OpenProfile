namespace OpenProfileServer.Constants;

public static class SystemSettingKeys
{
    // Policies
    public const string MaintenanceMode = "MaintenanceMode";
    public const string AllowRegistration = "AllowRegistration";
    public const string AllowSearchEngineIndexing = "AllowSearchEngineIndexing";
    
    /// <summary>
    /// If true, users must verify their email before they can access dynamic features.
    /// </summary>
    public const string RequireEmailVerification = "RequireEmailVerification";
    
    // Metadata
    public const string SiteName = "SiteName";
    public const string SiteDescription = "SiteDescription";
    public const string ContactEmail = "ContactEmail";

    /// <summary>
    /// Stores a serialized AssetDto representing the site logo.
    /// </summary>
    public const string SiteLogo = "SiteLogo";
    
    // Limits (Database overrides for appsettings)
    public const string DefaultStorageLimit = "DefaultStorageLimit";
}