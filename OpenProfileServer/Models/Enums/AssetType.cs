namespace OpenProfileServer.Models.Enums;

public enum AssetType
{
    /// <summary>
    /// Plain text or Emojis.
    /// </summary>
    Text,
    
    /// <summary>
    /// Base64 encoded image string.
    /// </summary>
    Image,
    
    /// <summary>
    /// Direct URL to a remote resource (http/https).
    /// </summary>
    Remote,
    
    /// <summary>
    /// CSS classes (e.g., 'fa-solid fa-user', 'devicon-csharp-plain').
    /// </summary>
    Style,
    
    /// <summary>
    /// Unique ID for an object storage resource (e.g., AWS S3 Key, Azure Blob ID).
    /// </summary>
    Identifier,
}