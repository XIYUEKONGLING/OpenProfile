namespace OpenProfileServer.Configuration;

public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// Current software version, typically injected during build or from appsettings.
    /// </summary>
    public string Version { get; set; } = "Unknown";
}