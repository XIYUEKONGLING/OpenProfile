using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Details;

/// <summary>
/// Represents a public certificate (e.g., OpenPGP, SSH key, S/MIME).
/// </summary>
public class Certificate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The type of the certificate (e.g., "OpenPGP", "SSH", "S/MIME", "X.509").
    /// Using string instead of Enum for better extensibility.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// The name/identity associated with the certificate (e.g., "User <user@example.com>").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier or hash of the certificate.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Fingerprint { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(128)]
    public string? Email { get; set; }

    /// <summary>
    /// The actual certificate content (e.g., ASCII Armored block).
    /// </summary>
    [MaxLength(65536)]
    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Visibility Visibility { get; set; } = Visibility.Public;
}