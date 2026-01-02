using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Site;

public class SiteMetadataDto
{
    public string SiteName { get; set; } = string.Empty;
    public string? SiteDescription { get; set; }
    public string? Copyright { get; set; }
    public string? ContactEmail { get; set; }
    public AssetDto Logo { get; set; } = new();
    public AssetDto Favicon { get; set; } = new();
}