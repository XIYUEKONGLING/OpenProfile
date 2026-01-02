using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class GalleryItemDto
{
    public Guid Id { get; set; }
    public AssetDto Image { get; set; } = new();
    public string? Caption { get; set; }
    public string? ActionUrl { get; set; }
    public int DisplayOrder { get; set; }
    public Visibility Visibility { get; set; }
}