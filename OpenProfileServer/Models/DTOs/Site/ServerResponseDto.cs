using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Site;

public class ServerResponseDto
{
    public ServerInfoDto ServerInfo { get; set; } = new();
    public SiteMetadataDto SiteMeta { get; set; } = new();
    public ServerFeaturesDto Features { get; set; } = new();
}