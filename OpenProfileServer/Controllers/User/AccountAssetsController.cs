using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Assets;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.User;

/// <summary>
/// Personal Asset Library Management (/api/me/assets).
/// Requires authentication.
/// </summary>
[Authorize]
[Route("api/me/assets")]
[ApiController]
public class AccountAssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AccountAssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    // ==========================================
    // Asset Management
    // ==========================================

    /// <summary>
    /// GET /api/me/assets
    /// List my assets (paginated).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AccountAssetDto>>>> GetMyAssets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] Visibility? visibility = null,
        [FromQuery] string? search = null)
    {
        return Ok(await _assetService.GetMyAssetsAsync(GetUserId(), page, pageSize, category, visibility, search));
    }

    /// <summary>
    /// GET /api/me/assets/{uuid}
    /// Get specific asset (my own).
    /// </summary>
    [HttpGet("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> GetMyAsset(Guid uuid)
    {
        var result = await _assetService.GetMyAssetAsync(GetUserId(), uuid);
        if (!result.Status)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// POST /api/me/assets
    /// Create new asset.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> CreateAsset([FromBody] CreateAccountAssetRequestDto dto)
    {
        var result = await _assetService.CreateMyAssetAsync(GetUserId(), dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/me/assets/{uuid}
    /// Full update asset.
    /// </summary>
    [HttpPut("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> UpdateAsset(Guid uuid, [FromBody] CreateAccountAssetRequestDto dto)
    {
        var result = await _assetService.UpdateMyAssetAsync(GetUserId(), uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/me/assets/{uuid}
    /// Partial update asset.
    /// </summary>
    [HttpPatch("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> PatchAsset(Guid uuid, [FromBody] UpdateAccountAssetRequestDto dto)
    {
        var result = await _assetService.PatchMyAssetAsync(GetUserId(), uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/me/assets/{uuid}
    /// Delete asset.
    /// </summary>
    [HttpDelete("{uuid}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteAsset(Guid uuid)
    {
        var result = await _assetService.DeleteMyAssetAsync(GetUserId(), uuid);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    // ==========================================
    // Batch Operations
    // ==========================================

    /// <summary>
    /// PATCH /api/me/assets/batch/visibility
    /// Batch update visibility.
    /// </summary>
    [HttpPatch("batch/visibility")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> BatchUpdateVisibility([FromBody] BatchUpdateVisibilityRequestDto dto)
    {
        var result = await _assetService.BatchUpdateMyAssetVisibilityAsync(GetUserId(), dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/me/assets/batch
    /// Batch delete assets.
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> BatchDelete([FromBody] BatchDeleteRequestDto dto)
    {
        var result = await _assetService.BatchDeleteMyAssetsAsync(GetUserId(), dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    // ==========================================
    // Categories
    // ==========================================

    /// <summary>
    /// GET /api/me/assets/categories
    /// List all my categories (distinct).
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetCategories()
    {
        return Ok(await _assetService.GetMyAssetCategoriesAsync(GetUserId()));
    }
}
