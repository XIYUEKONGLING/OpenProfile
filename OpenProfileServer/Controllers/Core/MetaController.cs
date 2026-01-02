using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Site;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Controllers.Core;

[Route("api")]
[ApiController]
public class MetaController : ControllerBase
{
    private readonly ISiteMetadataService _metadataService;
    private readonly ApplicationOptions _appOptions;

    public MetaController(ISiteMetadataService metadataService, IOptions<ApplicationOptions> appOptions)
    {
        _metadataService = metadataService;
        _appOptions = appOptions.Value;
    }

    /// <summary>
    /// GET /api
    /// Combined endpoint for both server info and site metadata.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ServerResponseDto>>> GetIndex()
    {
        var info = GetServerInfoInternal();
        var meta = await GetSiteMetaInternal();

        var response = new ServerResponseDto
        {
            ServerInfo = info,
            SiteMeta = meta
        };

        return Ok(ApiResponse<ServerResponseDto>.Success(response));
    }

    /// <summary>
    /// GET /api/info
    /// Standalone endpoint for server system information.
    /// </summary>
    [HttpGet("info")]
    public ActionResult<ApiResponse<ServerInfoDto>> GetInfo()
    {
        return Ok(ApiResponse<ServerInfoDto>.Success(GetServerInfoInternal()));
    }

    /// <summary>
    /// GET /api/meta
    /// Standalone endpoint for site branding metadata.
    /// </summary>
    [HttpGet("meta")]
    public async Task<ActionResult<ApiResponse<SiteMetadataDto>>> GetMeta()
    {
        return Ok(ApiResponse<SiteMetadataDto>.Success(await GetSiteMetaInternal()));
    }

    /// <summary>
    /// POST /api/admin/meta
    /// Full update of site metadata (Replace all fields).
    /// </summary>
    [Authorize(Roles = AccountRoles.AdminOrHigher)]
    [HttpPost("admin/meta")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateMeta([FromBody] UpdateSiteMetadataRequestDto dto)
    {
        var metadata = new SiteMetadata
        {
            SiteName = dto.SiteName ?? SiteMetadata.DefaultSiteName,
            SiteDescription = dto.SiteDescription,
            Copyright = dto.Copyright,
            ContactEmail = dto.ContactEmail,
            Logo = dto.Logo != null ? new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag } : new Asset(),
            Favicon = dto.Favicon != null ? new Asset { Type = dto.Favicon.Type, Value = dto.Favicon.Value, Tag = dto.Favicon.Tag } : new Asset()
        };

        await _metadataService.UpdateMetadataAsync(metadata);
        return Ok(ApiResponse<MessageResponse>.Success(MessageResponse.Create("Site metadata updated successfully.")));
    }

    /// <summary>
    /// PATCH /api/admin/meta
    /// Partial update of site metadata.
    /// </summary>
    [Authorize(Roles = AccountRoles.AdminOrHigher)]
    [HttpPatch("admin/meta")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> PatchMeta([FromBody] UpdateSiteMetadataRequestDto dto)
    {
        await _metadataService.PatchMetadataAsync(dto);
        return Ok(ApiResponse<MessageResponse>.Success(MessageResponse.Create("Site metadata partially updated.")));
    }

    private ServerInfoDto GetServerInfoInternal()
    {
        // These values are intrinsic to this specific assembly's nature.
        return new ServerInfoDto
        {
            Version = _appOptions.Version,
            Server = ApplicationInformation.ServerName,
            Static = false,
            Dynamic = true
        };
    }

    private async Task<SiteMetadataDto> GetSiteMetaInternal()
    {
        var metadata = await _metadataService.GetMetadataAsync();
        return new SiteMetadataDto
        {
            SiteName = metadata.SiteName,
            SiteDescription = metadata.SiteDescription,
            Copyright = metadata.Copyright,
            ContactEmail = metadata.ContactEmail,
            Logo = new AssetDto 
            { 
                Type = metadata.Logo.Type, 
                Value = metadata.Logo.Value, 
                Tag = metadata.Logo.Tag 
            },
            Favicon = new AssetDto 
            { 
                Type = metadata.Favicon.Type, 
                Value = metadata.Favicon.Value, 
                Tag = metadata.Favicon.Tag 
            }
        };
    }
}
