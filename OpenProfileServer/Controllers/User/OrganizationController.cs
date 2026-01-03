using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Controllers.User;

[Authorize]
[Route("api/orgs")]
[ApiController]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public OrganizationController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
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

    [HttpGet("{org}/settings")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> GetSettings(Guid org)
    {
        var result = await _orgService.GetOrgSettingsAsync(GetUserId(), org);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/settings")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSettings(Guid org, [FromBody] UpdateOrganizationSettingsRequestDto dto)
    {
        var result = await _orgService.UpdateOrgSettingsAsync(GetUserId(), org, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProfile(Guid org, [FromBody] UpdateProfileRequestDto dto)
    {
        var result = await _orgService.UpdateOrgProfileAsync(GetUserId(), org, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
    
    [HttpDelete("{org}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Delete(Guid org)
    {
        var result = await _orgService.DeleteOrganizationAsync(GetUserId(), org);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    // === Members & Invites ===

    [HttpGet("{org}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationMemberDto>>>> GetMembers(Guid org)
    {
        var result = await _orgService.GetMembersAsync(GetUserId(), org);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPost("{org}/members")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> InviteMember(Guid org, [FromBody] InviteMemberRequestDto dto)
    {
        var result = await _orgService.InviteMemberAsync(GetUserId(), org, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpDelete("{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> KickMember(Guid org, Guid user)
    {
        // "me" alias support
        if (user == Guid.Empty) user = GetUserId(); // Logic handled in LeaveOrganization if kicking self

        if (user == GetUserId())
        {
             return Ok(await _orgService.LeaveOrganizationAsync(GetUserId(), org));
        }
        
        var result = await _orgService.RemoveMemberAsync(GetUserId(), org, user);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpPatch("{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateMember(Guid org, Guid user, [FromBody] UpdateMemberRequestDto dto)
    {
        var result = await _orgService.UpdateMemberRoleAsync(GetUserId(), org, user, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpGet("{org}/invitations")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationInvitationDto>>>> GetOutboundInvitations(Guid org)
    {
        var result = await _orgService.GetPendingInvitationsAsync(GetUserId(), org);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    [HttpDelete("{org}/invitations/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RevokeInvitation(Guid org, Guid id)
    {
        var result = await _orgService.RevokeInvitationAsync(GetUserId(), org, id);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }
}
