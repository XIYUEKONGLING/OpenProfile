using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;

namespace OpenProfileServer.Controllers.User;

/// <summary>
/// Handles Personal Management (/api/me) endpoints.
/// </summary>
[Authorize]
[Route("api/me")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// GET /api/me
    /// Get full account details.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetMe()
    {
        return Ok(await _accountService.GetMyAccountAsync(GetUserId()));
    }

    /// <summary>
    /// GET /api/me/permissions
    /// Get current Account's Role and Type.
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<AccountPermissionsDto>>> GetMyPermissions()
    {
        return Ok(await _accountService.GetMyPermissionsAsync(GetUserId()));
    }

    /// <summary>
    /// GET /api/me/settings
    /// Get personal settings.
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<PersonalSettingsDto>>> GetSettings()
    {
        return Ok(await _accountService.GetMySettingsAsync(GetUserId()));
    }

    /// <summary>
    /// POST /api/me/settings
    /// Update settings (treated same as PATCH in this implementation for simplicity, but route exists for compatibility).
    /// </summary>
    [HttpPost("settings")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateSettings([FromBody] UpdatePersonalSettingsRequestDto dto)
    {
        return Ok(await _accountService.UpdateMySettingsAsync(GetUserId(), dto));
    }

    /// <summary>
    /// PATCH /api/me/settings
    /// Partial update settings.
    /// </summary>
    [HttpPatch("settings")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> PatchSettings([FromBody] UpdatePersonalSettingsRequestDto dto)
    {
        return Ok(await _accountService.UpdateMySettingsAsync(GetUserId(), dto));
    }

    /// <summary>
    /// GET /api/me/profile
    /// Get profile (Edit View).
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile()
    {
        return Ok(await _accountService.GetMyProfileAsync(GetUserId()));
    }

    /// <summary>
    /// POST /api/me/profile
    /// Full update profile (Logic shared with Patch).
    /// </summary>
    [HttpPost("profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        return Ok(await _accountService.UpdateMyProfileAsync(GetUserId(), dto));
    }

    /// <summary>
    /// PATCH /api/me/profile
    /// Partial update profile.
    /// </summary>
    [HttpPatch("profile")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> PatchProfile([FromBody] UpdateProfileRequestDto dto)
    {
        return Ok(await _accountService.UpdateMyProfileAsync(GetUserId(), dto));
    }

    /// <summary>
    /// POST /api/me/password
    /// Change password.
    /// </summary>
    [HttpPost("password")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var result = await _accountService.ChangePasswordAsync(GetUserId(), dto);
        if (!result.Status) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/me
    /// Request Account Deletion (Cooling-off).
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteMe()
    {
        var result = await _accountService.RequestDeletionAsync(GetUserId());
        if (!result.Status) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/me/restore
    /// Restore Account (Cancel deletion).
    /// </summary>
    [HttpPost("restore")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> RestoreMe()
    {
        var result = await _accountService.RestoreAccountAsync(GetUserId());
        if (!result.Status) return BadRequest(result);
        return Ok(result);
    }
}
