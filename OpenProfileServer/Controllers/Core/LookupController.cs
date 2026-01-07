using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Controllers.Core;

/// <summary>
/// Asset Lookup API for authenticated users.
/// Allows retrieving assets from user, organization, and system asset libraries
/// without knowing which library the asset belongs to.
/// </summary>
[Route("api/lookup")]
[ApiController]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly IAssetService _assetService;

    public LookupController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// GET /api/lookup/{uuid}
    /// Lookup asset by UUID across all asset libraries.
    /// Automatically searches in user assets, organization assets, and system assets.
    ///
    /// Returns the asset with permissions checked based on Visibility setting.
    ///
    /// Visibility Rules for Account Assets:
    /// - Public: Accessible by anyone
    /// - Authenticated: Accessible by any logged-in user
    /// - Protected: Accessible by any logged-in user
    /// - Private: Only the asset owner
    /// - FriendsOnly: Requires mutual follow relationship
    /// - MembersOnly: Organization assets: Only organization members
    ///
    /// Visibility Rules for System Assets:
    /// - Public: Accessible by anyone
    /// - Authenticated/Protected: Accessible by any logged-in user
    /// - Private/FriendsOnly/MembersOnly: Not applicable (returns 404)
    ///
    /// Returns 404 if asset not found, access denied, or belongs to suspended/banned/deleted account.
    /// </summary>
    [HttpGet("{uuid}")]
    [HttpGet("{uuid}.json")]
    public async Task<ActionResult<ApiResponse<LookupAssetDto>>> LookupAsset(Guid uuid)
    {
        var currentUserId = GetCurrentUserId(User);

        if (currentUserId == Guid.Empty)
            return Unauthorized();

        var result = await _assetService.LookupAssetAsync(uuid, currentUserId);
        if (!result.Status)
            return NotFound(result);

        return Ok(result);
    }
}
