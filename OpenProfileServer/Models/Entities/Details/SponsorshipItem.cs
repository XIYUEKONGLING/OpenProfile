using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class SponsorshipItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string Platform { get; set; } = string.Empty; // e.g., "Afdian", "PayPal", "Patreon"

    /// <summary>
    /// The URL for the sponsorship page.
    /// </summary>
    [MaxLength(512)]
    public string? Url { get; set; }

    /// <summary>
    /// Optional icon override for the platform.
    /// </summary>
    public Asset Icon { get; set; } = new();
    
    /// <summary>
    /// Optional QR code image for direct payments.
    /// </summary>
    public Asset QrCode { get; set; } = new();

    public int DisplayOrder { get; set; } = 0;

    public Visibility Visibility { get; set; } = Visibility.Public;
}