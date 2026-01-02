namespace OpenProfileServer.Constants;

public static class SystemSettingKeys
{
    // Policies
    public const string MaintenanceMode = "MaintenanceMode";
    public const string AllowRegistration = "AllowRegistration";
    public const string AllowSearchEngineIndexing = "AllowSearchEngineIndexing";
    
    // Metadata
    public const string SiteName = "SiteName";
    public const string SiteDescription = "SiteDescription";
    public const string ContactEmail = "ContactEmail";
    
    // Limits (Database overrides for appsettings)
    public const string DefaultStorageLimit = "DefaultStorageLimit";
}