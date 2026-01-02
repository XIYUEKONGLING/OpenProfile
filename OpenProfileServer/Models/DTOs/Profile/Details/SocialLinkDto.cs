using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class SocialLinkDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public AssetDto Icon { get; set; } = new();
}