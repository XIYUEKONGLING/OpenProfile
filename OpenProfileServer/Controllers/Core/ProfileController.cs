using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
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
    public async Task<ActionResult<ApiResponse<IEnumerable<SocialLinkDto>>>> GetSocials(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));
        
        return Ok(await _detailService.GetSocialsAsync(id.Value));
    }
    
    /// <summary>
    /// GET /api/profiles/{profile}/memberships
    /// Get organizations this user has joined publicly.
    /// </summary>
    [HttpGet("{profile}/memberships")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>>> GetMemberships(string profile)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        return Ok(await _detailService.GetPublicMembershipsAsync(id.Value));
    }

}
