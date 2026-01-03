using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Admin;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.Admin;

/// <summary>
/// Administrative endpoints.
/// Requires Admin or Root role.
/// </summary>
[Authorize(Roles = AccountRoles.AdminOrHigher)]
[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IProfileService _profileService;

    public AdminController(IAdminService adminService, IProfileService profileService)
    {
        _adminService = adminService;
        _profileService = profileService;
    }

    private Guid GetAdminId()
    {
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// GET /api/admin/users
    /// List users with pagination and filtering.
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserAdminDto>>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] AccountStatus? status = null,
        [FromQuery] AccountRole? role = null)
    {
        var pagination = new PaginationFilter { PageNumber = page, PageSize = pageSize };
        var filter = new UserFilterDto { Search = search, Status = status, Role = role };
        
        return Ok(await _adminService.GetUsersAsync(pagination, filter));
    }

    /// <summary>
    /// PATCH /api/admin/users/{user}/status
    /// Change account status (Active/Suspended/Banned).
    /// </summary>
    [HttpPatch("users/{user}/status")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateStatus(Guid user, [FromBody] UpdateUserStatusRequestDto dto)
    {
        var result = await _adminService.UpdateUserStatusAsync(GetAdminId(), user, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST /api/admin/users/{user}/role
    /// Promote/Demote system role.
    /// Requires Root role strictly.
    /// </summary>
    [Authorize(Roles = AccountRoles.Root)]
    [HttpPost("users/{user}/role")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateRole(Guid user, [FromBody] UpdateUserRoleRequestDto dto)
    {
        var result = await _adminService.UpdateUserRoleAsync(GetAdminId(), user, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// DELETE /api/admin/users/{user}
    /// Physical Delete (No cooling-off).
    /// Requires Root role strictly.
    /// </summary>
    [Authorize(Roles = AccountRoles.Root)]
    [HttpDelete("users/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteUser(Guid user)
    {
        var result = await _adminService.DeleteUserAsync(GetAdminId(), user);
        return result.Status ? Ok(result) : BadRequest(result);
    }
    
    /// <summary>
    /// POST /api/admin/users
    /// Force create user/org.
    /// If creating an Organization, the current Admin becomes the Owner.
    /// </summary>
    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<UserAdminDto>>> CreateUser([FromBody] CreateUserRequestDto dto)
    {
        var result = await _adminService.CreateUserAsync(GetAdminId(), dto);
        return result.Status ? Created("", result) : BadRequest(result);
    }
    
    // ==========================================
    // Organization Management (Admin Override)
    // ==========================================

    [HttpGet("orgs/{org}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationMemberDto>>>> GetOrgMembers(string org)
    {
        var orgId = await _profileService.ResolveIdAsync(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        return Ok(await _adminService.AdminGetOrgMembersAsync(orgId.Value));
    }

    [HttpPost("orgs/{org}/members")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> ForceAddMember(string org, [FromBody] InviteMemberRequestDto dto)
    {
        var orgId = await _profileService.ResolveIdAsync(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));
        
        // Resolve Target User (Identity can be username or email)
        var targetId = await _profileService.ResolveIdAsync(dto.Identity);
        // Note: ResolveIdAsync might not work for emails if not implemented, assuming Identity is AccountName/Handle here.
        // If logic requires email resolution, use a helper in AdminService.
        
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Target user not found."));

        return Ok(await _adminService.AdminAddMemberAsync(orgId.Value, targetId.Value, dto));
    }

    [HttpPatch("orgs/{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> ForceUpdateMember(string org, string user, [FromBody] UpdateMemberRequestDto dto)
    {
        var orgId = await _profileService.ResolveIdAsync(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var userId = await _profileService.ResolveIdAsync(user);
        if (userId == null) return NotFound(ApiResponse<MessageResponse>.Failure("User not found."));

        return Ok(await _adminService.AdminUpdateMemberAsync(orgId.Value, userId.Value, dto));
    }

    [HttpDelete("orgs/{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> ForceKickMember(string org, string user)
    {
        var orgId = await _profileService.ResolveIdAsync(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var userId = await _profileService.ResolveIdAsync(user);
        if (userId == null) return NotFound(ApiResponse<MessageResponse>.Failure("User not found."));

        return Ok(await _adminService.AdminKickMemberAsync(orgId.Value, userId.Value));
    }

}
