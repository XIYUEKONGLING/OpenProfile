using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Controllers.Core;

/// <summary>
/// Public Asset Library API (No authentication required).
/// Used by static generator and public access.
/// </summary>
[Route("api/assets")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }
    
    // TODO

    /// <summary>
    /// GET /api/assets/{uuid}
    /// Get public asset by UUID.
    /// Returns 404 if asset is private, deleted, or belongs to suspended/banned account.
    /// </summary>
    [HttpGet("{uuid}")]
    [HttpGet("{uuid}.json")]
    
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> GetAsset(Guid uuid)
    {
        var result = await _assetService.GetPublicAssetAsync(uuid);
        if (!result.Status)
        {
            return NotFound(result);
        }
        return Ok(result);
    }
}
