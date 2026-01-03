using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Profile.Details;
using OpenProfileServer.Models.DTOs.Social;

namespace OpenProfileServer.Controllers.Core;

/// <summary>
/// Public read-only endpoints for profiles.
/// Compatible with Static Generator.
/// </summary>
[Route("api/profiles")]
[ApiController]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ISocialService _socialService;
    private readonly IProfileDetailService _detailService;

    public ProfileController(
        IProfileService profileService, 
        ISocialService socialService,
        IProfileDetailService detailService)
    {
        _profileService = profileService;
        _socialService = socialService;
        _detailService = detailService;
    }

    /// <summary>
    /// GET /api/profiles/{profile}
    /// Get public profile info.
    /// Identifier can be username or @uuid.
    /// </summary>
    [HttpGet("{profile}")]
    [HttpGet("{profile}.json")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile(string profile)
    {
        var result = await _profileService.GetProfileAsync(profile);
        if (!result.Status) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/profiles/{profile}/followers
    /// Get list of followers.
    /// </summary>
    [HttpGet("{profile}/followers")]
    [HttpGet("{profile}/followers.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowers(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        return Ok(await _socialService.GetFollowersAsync(id.Value));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/following
    /// Get list of following.
    /// </summary>
    [HttpGet("{profile}/following")]
    [HttpGet("{profile}/following.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowing(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        return Ok(await _socialService.GetFollowingAsync(id.Value));
    }

    // ==========================================
    // Sub-Resources (Read-Only Public)
    // ==========================================
    
    /// <summary>
    /// GET /api/profiles/{profile}/work
    /// </summary>
    [HttpGet("{profile}/work")]
    [HttpGet("{profile}/work.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkExperienceDto>>>> GetWork(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        return Ok(await _detailService.GetWorkAsync(id.Value, publicOnly: true));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/education
    /// </summary>
    [HttpGet("{profile}/education")]
    [HttpGet("{profile}/education.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EducationExperienceDto>>>> GetEducation(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetEducationAsync(id.Value, publicOnly: true));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/projects
    /// </summary>
    [HttpGet("{profile}/projects")]
    [HttpGet("{profile}/projects.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetProjects(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetProjectsAsync(id.Value, publicOnly: true));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/socials
    /// </summary>
    [HttpGet("{profile}/socials")]
    [HttpGet("{profile}/socials.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SocialLinkDto>>>> GetSocials(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetSocialsAsync(id.Value));
    }
    
    /// <summary>
    /// GET /api/profiles/{profile}/gallery
    /// </summary>
    [HttpGet("{profile}/gallery")]
    [HttpGet("{profile}/gallery.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GalleryItemDto>>>> GetGallery(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetGalleryAsync(id.Value, publicOnly: true));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/certificates
    /// </summary>
    [HttpGet("{profile}/certificates")]
    [HttpGet("{profile}/certificates.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CertificateDto>>>> GetCertificates(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetCertificatesAsync(id.Value, publicOnly: true));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/sponsorships
    /// </summary>
    [HttpGet("{profile}/sponsorships")]
    [HttpGet("{profile}/sponsorships.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SponsorshipItemDto>>>> GetSponsorships(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetSponsorshipsAsync(id.Value, publicOnly: true));
    }
    
    /// <summary>
    /// GET /api/profiles/{profile}/memberships
    /// Get organizations this user has joined publicly.
    /// </summary>
    [HttpGet("{profile}/memberships")]
    [HttpGet("{profile}/memberships.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>>> GetMemberships(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        return Ok(await _detailService.GetPublicMembershipsAsync(id.Value));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/members
    /// Get public members of an organization.
    /// </summary>
    [HttpGet("{profile}/members")]
    [HttpGet("{profile}/members.json")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrganizationMemberDto>>>> GetMembers(string profile)
    {
        var orgId = await _profileService.ResolveIdAsync(profile);
        if (orgId == null) return NotFound(ApiResponse<string>.Failure("Organization not found."));

        return Ok(await _detailService.GetPublicOrgMembersAsync(orgId.Value));
    }
}
