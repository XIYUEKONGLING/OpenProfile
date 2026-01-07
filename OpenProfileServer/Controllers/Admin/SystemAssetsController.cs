using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Assets;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.Admin;

/// <summary>
/// System Asset Management (Admin Only).
/// Requires Admin or Root role.
/// </summary>
[Authorize(Roles = AccountRoles.AdminOrHigher)]
[Route("api/admin/assets")]
[ApiController]
public class SystemAssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public SystemAssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    /// <summary>
    /// GET /api/admin/assets
    /// List all system assets (paginated).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<SystemAssetDto>>>> GetSystemAssets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] Visibility? visibility = null)
    {
        return Ok(await _assetService.GetSystemAssetsAsync(page, pageSize, category, visibility));
    }

    /// <summary>
    /// GET /api/admin/assets/{uuid}
    /// Get specific system asset.
    /// </summary>
    [HttpGet("{uuid}")]
    public async Task<ActionResult<ApiResponse<SystemAssetDto>>> GetSystemAsset(Guid uuid)
    {
        var result = await _assetService.GetSystemAssetAsync(uuid);
        if (!result.Status)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// POST /api/admin/assets
    /// Create system asset.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemAssetDto>>> CreateSystemAsset([FromBody] CreateSystemAssetRequestDto dto)
    {
        var result = await _assetService.CreateSystemAssetAsync(dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetSystemAsset), new { uuid = result.Data!.Id }, result);
    }

    /// <summary>
    /// PUT /api/admin/assets/{uuid}
    /// Full update system asset.
    /// </summary>
    [HttpPut("{uuid}")]
    public async Task<ActionResult<ApiResponse<SystemAssetDto>>> UpdateSystemAsset(Guid uuid, [FromBody] CreateSystemAssetRequestDto dto)
    {
        var result = await _assetService.UpdateSystemAssetAsync(uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/admin/assets/{uuid}
    /// Partial update system asset.
    /// </summary>
    [HttpPatch("{uuid}")]
    public async Task<ActionResult<ApiResponse<SystemAssetDto>>> PatchSystemAsset(Guid uuid, [FromBody] UpdateSystemAssetRequestDto dto)
    {
        var result = await _assetService.PatchSystemAssetAsync(uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/admin/assets/{uuid}
    /// Delete system asset.
    /// </summary>
    [HttpDelete("{uuid}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteSystemAsset(Guid uuid)
    {
        var result = await _assetService.DeleteSystemAssetAsync(uuid);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
