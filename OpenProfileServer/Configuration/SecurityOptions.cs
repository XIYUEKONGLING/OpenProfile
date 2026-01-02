namespace OpenProfileServer.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Username for the system super-admin.
    /// </summary>
    public string RootUser { get; set; } = string.Empty;

    /// <summary>
    /// Password for the system super-admin.
    /// </summary>
    public string RootPassword { get; set; } = string.Empty;

    /// <summary>
    /// Secret key for JWT signing or other encryption operations.
    /// </summary>
    public string ApplicationSecret { get; set; } = string.Empty;
}