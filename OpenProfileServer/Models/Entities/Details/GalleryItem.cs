using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class GalleryItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The actual image asset.
    /// </summary>
    [Required]
    public Asset Image { get; set; } = new();

    [MaxLength(256)]
    public string? Caption { get; set; }

    /// <summary>
    /// Optional link when the user clicks the image.
    /// </summary>
    [MaxLength(512)]
    public string? ActionUrl { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public Visibility Visibility { get; set; } = Visibility.Public;
}