using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Profile.Details;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.User;

[Authorize]
[Route("api/orgs")]
[ApiController]
public class OrganizationController : ControllerBase
{

    private readonly IOrganizationService _orgService;
    private readonly IProfileService _profileService; 
    private readonly IProfileDetailService _detailService;

    public OrganizationController(
        IOrganizationService orgService, 
        IProfileService profileService,
        IProfileDetailService detailService)
    {
        _orgService = orgService;
        _profileService = profileService;
        _detailService = detailService;
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
        
        // Only Owner and Admin can manage sub-resources
        if (roleResult.Status && (roleResult.Data.Role == MemberRole.Owner || roleResult.Data.Role == MemberRole.Admin))
        {
            return orgId;
        }
        return null;
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
    
    /// <summary>
    /// GET /api/orgs/{org}/profile
    /// Get full profile details for editing.
    /// Accessible by all members.
    /// </summary>
    [HttpGet("{org}/profile")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        // Service internally checks if the user is a member
        var result = await _orgService.GetOrgProfileAsync(GetUserId(), orgId.Value);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    /// <summary>
    /// POST /api/orgs/{org}/profile
    /// Full update of the organization profile.
    /// </summary>
    [HttpPost("{org}/profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProfile(string org, [FromBody] UpdateProfileRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.UpdateOrgProfileAsync(GetUserId(), orgId.Value, dto);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }

    /// <summary>
    /// PATCH /api/orgs/{org}/profile
    /// Partial update of the organization profile.
    /// </summary>
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

    /// <summary>
    /// GET /api/orgs/{org}/permissions
    /// Returns the current member's permissions in the organization.
    /// </summary>
    [HttpGet("{org}/permissions")]
    public async Task<ActionResult<ApiResponse<OrganizationPermissionsDto>>> GetMyPermissions(string org)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        var result = await _orgService.GetMyPermissionsAsync(GetUserId(), orgId.Value);
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
    public async Task<ActionResult<ApiResponse<MessageResponse>>> KickMember(string org, string user)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));

        Guid targetUserId;

        // 1. Handle 'me' shortcut (Case-insensitive)
        if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            targetUserId = GetUserId();
        } 
        // 2. Handle UUID
        else if (Guid.TryParse(user, out var parsedGuid))
        {
            targetUserId = parsedGuid;
        }
        // 3. Invalid format
        else
        {
            return BadRequest(ApiResponse<MessageResponse>.Failure("Invalid user identifier. Use 'me' or a valid UUID."));
        }

        // Logic to leave or kick
        if (targetUserId == GetUserId())
        {
            return Ok(await _orgService.LeaveOrganizationAsync(GetUserId(), orgId.Value));
        }
        
        var result = await _orgService.RemoveMemberAsync(GetUserId(), orgId.Value, targetUserId);
        return result.Status ? Ok(result) : StatusCode(403, result);
    }


    [HttpPatch("{org}/members/{user}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateMember(string org, string user, [FromBody] UpdateMemberRequestDto dto)
    {
        var orgId = await ResolveOrgId(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found."));
        
        // This endpoint is intended for managing others (Role updates).
        // 'me' is handled by the separate 'UpdateMyMemberDetails' endpoint.
        // We validate the UUID here to provide a clear error message instead of a generic 400 binding error.
        if (!Guid.TryParse(user, out var targetUserId))
        {
            return BadRequest(ApiResponse<MessageResponse>.Failure("Invalid user identifier. Expected a valid UUID."));
        }
        
        var result = await _orgService.UpdateMemberRoleAsync(GetUserId(), orgId.Value, targetUserId, dto);
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
    
    // ==========================================
    // Sub-Resources Management (Projects, Socials, etc.)
    // ==========================================
    
    // --- Projects ---

    [HttpGet("{org}/projects")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetProjects(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        // publicOnly = false because this is the management view
        return Ok(await _detailService.GetProjectsAsync(orgId.Value, publicOnly: false));
    }

    [HttpPost("{org}/projects")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddProject(string org, [FromBody] UpdateProjectRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddProjectAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/projects/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProject(string org, Guid id, [FromBody] UpdateProjectRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateProjectAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/projects/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteProject(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteProjectAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // --- Socials ---

    [HttpGet("{org}/socials")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SocialLinkDto>>>> GetSocials(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        return Ok(await _detailService.GetSocialsAsync(orgId.Value));
    }

    [HttpPost("{org}/socials")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddSocial(string org, [FromBody] UpdateSocialLinkRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddSocialAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/socials/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSocial(string org, Guid id, [FromBody] UpdateSocialLinkRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateSocialAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/socials/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteSocial(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteSocialAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // --- Contacts ---

    [HttpGet("{org}/contacts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContactMethodDto>>>> GetContacts(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        return Ok(await _detailService.GetContactsAsync(orgId.Value, publicOnly: false));
    }

    [HttpPost("{org}/contacts")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddContact(string org, [FromBody] UpdateContactMethodRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddContactAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/contacts/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateContact(string org, Guid id, [FromBody] UpdateContactMethodRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateContactAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/contacts/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteContact(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteContactAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // --- Gallery ---

    [HttpGet("{org}/gallery")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GalleryItemDto>>>> GetGallery(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        return Ok(await _detailService.GetGalleryAsync(orgId.Value, publicOnly: false));
    }

    [HttpPost("{org}/gallery")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddGalleryItem(string org, [FromBody] UpdateGalleryItemRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddGalleryItemAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/gallery/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateGalleryItem(string org, Guid id, [FromBody] UpdateGalleryItemRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateGalleryItemAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/gallery/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteGalleryItem(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteGalleryItemAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // --- Certificates ---

    [HttpGet("{org}/certificates")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CertificateDto>>>> GetCertificates(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        return Ok(await _detailService.GetCertificatesAsync(orgId.Value, publicOnly: false));
    }

    [HttpPost("{org}/certificates")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddCertificate(string org, [FromBody] UpdateCertificateRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddCertificateAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/certificates/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateCertificate(string org, Guid id, [FromBody] UpdateCertificateRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateCertificateAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/certificates/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteCertificate(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteCertificateAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }

    // --- Sponsorships ---

    [HttpGet("{org}/sponsorships")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SponsorshipItemDto>>>> GetSponsorships(string org)
    {
        var orgId = await CheckMemberPermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        return Ok(await _detailService.GetSponsorshipsAsync(orgId.Value, publicOnly: false));
    }

    [HttpPost("{org}/sponsorships")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> AddSponsorship(string org, [FromBody] UpdateSponsorshipItemRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.AddSponsorshipAsync(orgId.Value, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{org}/sponsorships/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSponsorship(string org, Guid id, [FromBody] UpdateSponsorshipItemRequestDto dto)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.UpdateSponsorshipAsync(orgId.Value, id, dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{org}/sponsorships/{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteSponsorship(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));
        var result = await _detailService.DeleteSponsorshipAsync(orgId.Value, id);
        return result.Status ? Ok(result) : NotFound(result);
    }
    
    private ActionResult ForbiddenResponse() => 
        StatusCode(403, ApiResponse<MessageResponse>.Failure("Insufficient permissions. Owner or Admin role required."));
}
