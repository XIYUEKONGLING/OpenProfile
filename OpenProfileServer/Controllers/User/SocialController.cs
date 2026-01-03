using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Social;

namespace OpenProfileServer.Controllers.User;

/// <summary>
/// Handles authenticated social actions.
/// Routes are mapped under /api/profiles to match the REST resource logic.
/// </summary>
[Authorize]
[Route("api/profiles")]
[ApiController]
public class SocialController : ControllerBase
{
    private readonly ISocialService _socialService;
    private readonly IProfileService _profileService;

    public SocialController(ISocialService socialService, IProfileService profileService)
    {
        _socialService = socialService;
        _profileService = profileService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// POST /api/profiles/{profile}/follow
    /// Follow a user.
    /// </summary>
    [HttpPost("{profile}/follow")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Follow(string profile)
    {
        var targetId = await _profileService.ResolveIdAsync(profile);
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Profile not found."));

        return Ok(await _socialService.FollowUserAsync(GetUserId(), targetId.Value));
    }

    /// <summary>
    /// DELETE /api/profiles/{profile}/follow
    /// Unfollow a user.
    /// </summary>
    [HttpDelete("{profile}/follow")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Unfollow(string profile)
    {
        var targetId = await _profileService.ResolveIdAsync(profile);
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Profile not found."));

        return Ok(await _socialService.UnfollowUserAsync(GetUserId(), targetId.Value));
    }

    /// <summary>
    /// GET /api/profiles/{profile}/follow
    /// Check follow status between Me and Target.
    /// </summary>
    [HttpGet("{profile}/follow")]
    public async Task<ActionResult<ApiResponse<FollowStatusDto>>> GetStatus(string profile)
    {
        var targetId = await _profileService.ResolveIdAsync(profile);
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Profile not found."));

        return Ok(await _socialService.GetFollowStatusAsync(GetUserId(), targetId.Value));
    }

    /// <summary>
    /// POST /api/profiles/{profile}/block
    /// Block a user.
    /// </summary>
    [HttpPost("{profile}/block")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Block(string profile)
    {
        var targetId = await _profileService.ResolveIdAsync(profile);
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Profile not found."));

        return Ok(await _socialService.BlockUserAsync(GetUserId(), targetId.Value));
    }

    /// <summary>
    /// DELETE /api/profiles/{profile}/block
    /// Unblock a user.
    /// </summary>
    [HttpDelete("{profile}/block")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Unblock(string profile)
    {
        var targetId = await _profileService.ResolveIdAsync(profile);
        if (targetId == null) return NotFound(ApiResponse<MessageResponse>.Failure("Profile not found."));

        return Ok(await _socialService.UnblockUserAsync(GetUserId(), targetId.Value));
    }
}
