using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities;

/// <summary>
/// System-wide global asset library. Reserved for system use only.
/// Stores shared resources like default avatars, system icons, etc.
/// </summary>
public class SystemAsset
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// System-defined category for organizing assets.
    /// </summary>
    [MaxLength(128)]
    public string? Category { get; set; }

    /// <summary>
    /// System notes describing the asset's purpose and usage.
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
    public Visibility Visibility { get; set; } = Visibility.Public;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
