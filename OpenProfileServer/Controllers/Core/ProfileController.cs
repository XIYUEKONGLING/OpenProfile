using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Profile.Details;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Enums;
using ZiggyCreatures.Caching.Fusion;

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
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;

    public ProfileController(
        IProfileService profileService, 
        ISocialService socialService,
        IProfileDetailService detailService,
        ApplicationDbContext context,
        IFusionCache cache)
    {
        _profileService = profileService;
        _socialService = socialService;
        _detailService = detailService;
        _context = context;
        _cache = cache;
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

        var cacheKey = CacheKeys.OrganizationMembers(orgId.Value);

        var members = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            // Only return members who have set their visibility to Public
            // And ensure the member account itself is Active
            return await _context.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId.Value)
                .Where(m => m.Visibility == Visibility.Public)
                .Include(m => m.Account).ThenInclude(a => a.Profile)
                .Where(m => m.Account.Status == AccountStatus.Active)
                .Select(m => new OrganizationMemberDto
                {
                    AccountId = m.AccountId,
                    AccountName = m.Account.AccountName,
                    DisplayName = m.Account.Profile != null ? m.Account.Profile.DisplayName : "",
                    Avatar = m.Account.Profile != null ? new Models.DTOs.Core.AssetDto 
                    { 
                         Type = m.Account.Profile.Avatar.Type, 
                         Value = m.Account.Profile.Avatar.Value 
                    } : new Models.DTOs.Core.AssetDto(),
                    Role = m.Role,
                    Title = m.Title,
                    Visibility = m.Visibility,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return Ok(ApiResponse<IEnumerable<OrganizationMemberDto>>.Success(members ?? new List<OrganizationMemberDto>()));
    }
}
