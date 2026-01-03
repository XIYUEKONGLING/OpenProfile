namespace OpenProfileServer.Configuration;

public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// List of allowed origins. Use "*" for any origin (credentials will be disabled).
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}