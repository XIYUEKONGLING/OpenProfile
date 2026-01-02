using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Site;

public class UpdateSiteMetadataRequestDto
{
    public string? SiteName { get; set; }
    public string? SiteDescription { get; set; }
    public string? Copyright { get; set; }
    public string? ContactEmail { get; set; }
    public AssetDto? Logo { get; set; }
    public AssetDto? Favicon { get; set; }
}