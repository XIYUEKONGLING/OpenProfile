using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class SponsorshipItemDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? Url { get; set; }
    public AssetDto Icon { get; set; } = new();
    public AssetDto QrCode { get; set; } = new();
    public int DisplayOrder { get; set; }
    public Visibility Visibility { get; set; }
}