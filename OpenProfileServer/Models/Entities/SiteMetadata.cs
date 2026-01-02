using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities;

public class SiteMetadata
{
    public const string DefaultSiteName = "OpenProfile";
    
    [Key]
    public int Id { get; set; } = 1;

    [Required]
    [MaxLength(128)]
    public string SiteName { get; set; } = DefaultSiteName;

    [MaxLength(512)]
    public string? SiteDescription { get; set; }

    [MaxLength(256)]
    public string? Copyright { get; set; }

    [EmailAddress]
    [MaxLength(128)]
    public string? ContactEmail { get; set; }

    public Asset Logo { get; set; } = new();
    public Asset Favicon { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}