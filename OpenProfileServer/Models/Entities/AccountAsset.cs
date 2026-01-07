using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities;

/// <summary>
/// User asset library. Stores reusable assets (images, icons, etc.)
/// that can be referenced across profiles, posts, and other content.
/// </summary>
public class AccountAsset
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    /// <summary>
    /// User-defined category/folder name for organizing assets.
    /// </summary>
    [MaxLength(128)]
    public string? Category { get; set; }

    /// <summary>
    /// User notes describing where and how this asset is used.
    /// </summary>
    [MaxLength(512)]
    public string? Notes { get; set; }

    /// <summary>
    /// The complete Asset object (nested storage).
    /// </summary>
    [Required]
    public Asset Asset { get; set; } = new();

    /// <summary>
    /// Visibility setting for this asset.
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Private;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
