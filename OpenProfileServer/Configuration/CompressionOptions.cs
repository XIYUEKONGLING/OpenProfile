namespace OpenProfileServer.Configuration;

public class CompressionOptions
{
    public const string SectionName = "Compression";

    public bool Enabled { get; set; } = true;
    public bool EnableForHttps { get; set; } = true;
    
    /// <summary>
    /// Compression level: "Fastest", "Optimal", "SmallestSize", or "NoCompression".
    /// </summary>
    public string Level { get; set; } = "Fastest";
}