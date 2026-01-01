using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class SocialLink
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string Platform { get; set; } = string.Empty; // e.g. "GitHub"

    [Required]
    [MaxLength(512)]
    public string Url { get; set; } = string.Empty;

    // Optional icon override using your Asset system (e.g., specific SVG or FontAwesome class)
    public Asset Icon { get; set; } = new();
}