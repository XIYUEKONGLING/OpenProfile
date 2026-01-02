namespace OpenProfileServer.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public long MaxUploadSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB

    public string[] AllowedMimeTypes { get; set; } = 
    [ 
        "image/jpeg", 
        "image/png", 
        "image/webp", 
        "image/gif",
    ];
}