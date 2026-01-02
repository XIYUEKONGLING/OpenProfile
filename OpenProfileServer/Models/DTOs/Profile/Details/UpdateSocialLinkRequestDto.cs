using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateSocialLinkRequestDto
{
    public string? Platform { get; set; }
    public string? Url { get; set; }
    public AssetDto? Icon { get; set; }
}