using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;
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

    public ProfileController(IProfileService profileService, ISocialService socialService)
    {
        _profileService = profileService;
        _socialService = socialService;
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
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowers(string profile, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        var filter = new PaginationFilter { PageNumber = page, PageSize = pageSize };
        // Note: Real implementation should check Settings.ShowFollowersList here.
        
        return Ok(await _socialService.GetFollowersAsync(id.Value, filter));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/following
    /// Get list of following.
    /// </summary>
    [HttpGet("{profile}/following")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerDto>>>> GetFollowing(string profile, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var id = await _profileService.ResolveIdAsync(profile);
        if (id == null) return NotFound(ApiResponse<string>.Failure("Profile not found."));

        var filter = new PaginationFilter { PageNumber = page, PageSize = pageSize };
        // Note: Real implementation should check Settings.ShowFollowingList here.
        
        return Ok(await _socialService.GetFollowingAsync(id.Value, filter));
    }
}
