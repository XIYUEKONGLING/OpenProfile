using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateGalleryItemRequestDto
{
    public AssetDto? Image { get; set; }
    public string? Caption { get; set; }
    public string? ActionUrl { get; set; }
    public int? DisplayOrder { get; set; }
    public Visibility? Visibility { get; set; }
}