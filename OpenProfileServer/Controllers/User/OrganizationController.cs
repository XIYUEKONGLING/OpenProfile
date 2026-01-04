using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Controllers.User;

[Authorize]
[Route("api/orgs")]
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _orgService;
    private readonly IProfileService _profileService; 

    public OrganizationController(IOrganizationService orgService, IProfileService profileService)
    {
        _orgService = orgService;
        _profileService = profileService;
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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationDto>>>> GetMyOrgs()
    {
        return Ok(await _orgService.GetMyOrganizationsAsync(GetUserId()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateOrganizationRequestDto dto)
    {
        var result = await _orgService.CreateOrganizationAsync(GetUserId(), dto);
        return result.Status ? Created("", result) : BadRequest(result);
    }

    // === Management ===

    [HttpGet("{org}")]
    public async Task<ActionResult<ApiResponse<OrganizationDto>>> GetDashboard(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetOrganizationAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpGet("{org}/settings")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> GetSettings(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetOrgSettingsAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPost("{org}/settings")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSettings(string org, [FromBody] UpdateOrganizationSettingsRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.UpdateOrgSettingsAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/settings")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> PatchSettings(string org, [FromBody] UpdateOrganizationSettingsRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.PatchOrgSettingsAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPost("{org}/profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProfile(string org, [FromBody] UpdateProfileRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.UpdateOrgProfileAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> PatchProfile(string org, [FromBody] UpdateProfileRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.PatchOrgProfileAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
    
    [HttpDelete("{org}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Delete(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.DeleteOrganizationAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPost("{org}/restore")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Restore(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.RestoreOrganizationAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
    
    /// <summary>
    /// GET /api/orgs/{org}/follow-stats
    /// Get organization follow counts (Member access required).
    /// </summary>
    [HttpGet("{org}/follow-stats")]
    public async Task<ActionResult<ApiResponse<FollowCountsDto>>> GetFollowStats(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetOrgFollowCountsAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
    
    /// <summary>
    /// GET /api/orgs/{org}/followers
    /// Get organization followers list (Member access required, ignores privacy).
    /// </summary>
    [HttpGet("{org}/followers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowers(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetOrgFollowersAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    /// <summary>
    /// GET /api/orgs/{org}/following
    /// Get organization following list (Member access required, ignores privacy).
    /// </summary>
    [HttpGet("{org}/following")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowing(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetOrgFollowingAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    // === Members & Invites ===

    [HttpGet("{org}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationMemberDto>>>> GetMembers(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetMembersAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpGet("{org}/members/me/role")]
    public async Task<ActionResult<ApiResponse<MemberRoleDto>>> GetMyRole(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetMyRoleAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/members/me")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateMyMemberDetails(string org, [FromBody] UpdateMemberRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.UpdateMyMemberDetailsAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPost("{org}/members")]
    [HttpPost("{org}/invitations")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> InviteMember(string org, [FromBody] InviteMemberRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.InviteMemberAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpDelete("{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> KickMember(string org, Guid user)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        if (user == Guid.Empty) user = GetUserId(); 

        if (user == GetUserId())
        {
             return Ok(await _orgService.LeaveOrganizationAsync(GetUserId(), orgId.Value));
        }
        
        var result = await _orgService.RemoveMemberAsync(GetUserId(), orgId.Value, user);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateMember(string org, Guid user, [FromBody] UpdateMemberRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.UpdateMemberRoleAsync(GetUserId(), orgId.Value, user, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpGet("{org}/invitations")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationInvitationDto>>>> GetOutboundInvitations(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetPendingInvitationsAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpDelete("{org}/invitations/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RevokeInvitation(string org, Guid id)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.RevokeInvitationAsync(GetUserId(), orgId.Value, id);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
}
