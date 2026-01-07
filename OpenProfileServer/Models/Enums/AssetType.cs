namespace OpenProfileServer.Models.Enums;

public enum AssetType
{
    Empty = 0,

    /// <summary>
    /// Plain text or Emojis.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Base64 encoded image string.
    /// </summary>
    Image = 2,

    /// <summary>
    /// Direct URL to a remote resource (http/https).
    /// </summary>
    Remote = 3,

    /// <summary>
    /// CSS classes (e.g., 'fa-solid fa-user', 'devicon-csharp-plain').
    /// </summary>
    Style = 4,

    /// <summary>
    /// Unique ID for an object storage resource (e.g., AWS S3 Key, Azure Blob ID).
    /// System reserved, not available for user use.
    /// </summary>
    Identifier = 5,

    /// <summary>
    /// System reserved, not available for user use.
    /// </summary>
    System = 6,
    
    /// <summary>
    /// Reference to user asset library (AccountAsset.Id).
    /// When set, Asset.Value contains the UUID of the AccountAsset.
    /// </summary>
    Library = 7,

    /// <summary>
    /// Reference to system global asset library (SystemAsset.Id).
    /// When set, Asset.Value contains the UUID of the SystemAsset.
    /// </summary>
    Resource = 8,
}