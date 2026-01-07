using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Assets;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.Organization;

/// <summary>
/// Organization Asset Library Management (/api/orgs/{org}/assets).
/// Requires authentication and organization membership.
/// </summary>
[Authorize]
[Route("api/orgs/{org}/assets")]
[ApiController]
public class OrganizationAssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IProfileService _profileService;
    private readonly IOrganizationService _orgService;

    public OrganizationAssetsController(
        IAssetService assetService,
        IProfileService profileService,
        IOrganizationService orgService)
    {
        _assetService = assetService;
        _profileService = profileService;
        _orgService = orgService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    private async Task<Guid?> ResolveOrgId(string identifier)
    {
        return await _profileService.ResolveIdAsync(identifier);
    }

    /// <summary>
    /// Helper to check if current user is ANY member of the org.
    /// Used for Read-Only edit views.
    /// </summary>
    private async Task<Guid?> CheckMemberPermission(string orgIdentifier)
    {
        var orgId = await ResolveOrgId(orgIdentifier);
        if (orgId == null) return null;

        var roleResult = await _orgService.GetMyRoleAsync(GetUserId(), orgId.Value);
        return roleResult.Status ? orgId : null;
    }

    /// <summary>
    /// Helper to check if current user is Owner or Admin of the org.
    /// Used for Management (Write) operations.
    /// </summary>
    private async Task<Guid?> CheckManagePermission(string orgIdentifier)
    {
        var orgId = await ResolveOrgId(orgIdentifier);
        if (orgId == null) return null;

        var roleResult = await _orgService.GetMyRoleAsync(GetUserId(), orgId.Value);
        if (roleResult.Data == null)
        {
            return null;
        }

        // Only Owner and Admin can manage assets
        if (roleResult.Status && (roleResult.Data.Role == MemberRole.Owner || roleResult.Data.Role == MemberRole.Admin))
        {
            return orgId;
        }
        return null;
    }

    // ==========================================
    // Asset Management
    // ==========================================

    /// <summary>
    /// GET /api/orgs/{org}/assets
    /// List organization assets (paginated). Member access required.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<AccountAssetDto>>>> GetOrganizationAssets(
        string org,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] Visibility? visibility = null,
        [FromQuery] string? search = null)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _assetService.GetOrganizationAssetsAsync(orgId.Value, page, pageSize, category, visibility, search));
    }

    /// <summary>
    /// GET /api/orgs/{org}/assets/{uuid}
    /// Get specific organization asset. Member access required.
    /// </summary>
    [HttpGet("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> GetOrganizationAsset(string org, Guid uuid)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.GetOrganizationAssetAsync(orgId.Value, uuid);
        if (!result.Status)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// POST /api/orgs/{org}/assets
    /// Create new organization asset. Owner/Admin access required.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> CreateOrganizationAsset(
        string org,
        [FromBody] CreateAccountAssetRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.CreateOrganizationAssetAsync(orgId.Value, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/orgs/{org}/assets/{uuid}
    /// Full update organization asset. Owner/Admin access required.
    /// </summary>
    [HttpPut("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> UpdateOrganizationAsset(
        string org,
        Guid uuid,
        [FromBody] CreateAccountAssetRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.UpdateOrganizationAssetAsync(orgId.Value, uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/orgs/{org}/assets/{uuid}
    /// Partial update organization asset. Owner/Admin access required.
    /// </summary>
    [HttpPatch("{uuid}")]
    public async Task<ActionResult<ApiResponse<AccountAssetDto>>> PatchOrganizationAsset(
        string org,
        Guid uuid,
        [FromBody] UpdateAccountAssetRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.PatchOrganizationAssetAsync(orgId.Value, uuid, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/orgs/{org}/assets/{uuid}
    /// Delete organization asset. Owner/Admin access required.
    /// </summary>
    [HttpDelete("{uuid}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteOrganizationAsset(string org, Guid uuid)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.DeleteOrganizationAssetAsync(orgId.Value, uuid);
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
    /// PATCH /api/orgs/{org}/assets/batch/visibility
    /// Batch update organization asset visibility. Owner/Admin access required.
    /// </summary>
    [HttpPatch("batch/visibility")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> BatchUpdateVisibility(
        string org,
        [FromBody] BatchUpdateVisibilityRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.BatchUpdateOrganizationAssetVisibilityAsync(orgId.Value, dto);
        if (!result.Status)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/orgs/{org}/assets/batch
    /// Batch delete organization assets. Owner/Admin access required.
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> BatchDelete(
        string org,
        [FromBody] BatchDeleteRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var result = await _assetService.BatchDeleteOrganizationAssetsAsync(orgId.Value, dto);
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
    /// GET /api/orgs/{org}/assets/categories
    /// List all organization categories (distinct). Member access required.
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetCategories(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _assetService.GetOrganizationAssetCategoriesAsync(orgId.Value));
    }
}
