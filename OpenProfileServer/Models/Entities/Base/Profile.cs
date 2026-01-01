using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Details;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Base;

/// <summary>
/// Abstract base class for all profile types.
/// Maps to the 'Profiles' table.
/// </summary>
public abstract class Profile
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public virtual Account Account { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Short bio or description.
    /// </summary>
    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// Full README / Markdown content.
    /// </summary>
    public string? Content { get; set; }

    [MaxLength(128)]
    public string? Location { get; set; }

    [MaxLength(256)]
    public string? Website { get; set; }

    // Owned Entity: Maps to Avatar_Type, Avatar_Value, Avatar_Tag columns
    public Asset Avatar { get; set; } = new();

    // Owned Entity: Maps to Background_Type...
    public Asset Background { get; set; } = new();

    // Common Collections
    public virtual ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();
    public virtual ICollection<ContactMethod> Contacts { get; set; } = new List<ContactMethod>();
}