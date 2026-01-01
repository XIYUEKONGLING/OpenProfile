using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Summary { get; set; }
    
    [MaxLength(16384)]
    public string? Content { get; set; } // Description

    /// <summary>
    /// Link to the actual project (e.g., GitHub repo, Website, App Store).
    /// </summary>
    [MaxLength(512)]
    public string? Url { get; set; }

    /// <summary>
    /// Project logo or branding icon.
    /// </summary>
    public Asset Logo { get; set; } = new();

    /// <summary>
    /// Optional order for display.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public Visibility Visibility { get; set; } = Visibility.Public;
}