using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenProfileServer.Constants;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Auth;
using OpenProfileServer.Models.DTOs.Common;

namespace OpenProfileServer.Controllers.Auth;

/// <summary>
/// Handles authentication, registration, token management, and session revocation.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    /// <summary>
    /// GET /api/auth/config
    /// Check registration requirements (e.g. is email verification required?).
    /// </summary>
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<AuthConfigDto>>> GetConfig()
    {
        return Ok(await _authService.GetConfigAsync());
    }
    
    /// <summary>
    /// POST /api/auth/send-code
    /// Send a verification code to the provided email. 
    /// Used for Registration, Password Reset, or Adding Emails.
    /// </summary>
    [HttpPost("send-code")]
    [EnableRateLimiting(RateLimitPolicies.Email)]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> SendCode([FromBody] SendCodeRequestDto dto)
    {
        var result = await _authService.SendCodeAsync(dto);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST /api/auth/register
    /// Register a new account. If config requires email, 'code' must be provided.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        
        if (!result.Status) return BadRequest(result);
        
        return Created("", result); // 201 Created & Logged In
    }


    /// <summary>
    /// Authenticates a user and returns a set of JWT tokens.
    /// Rate limited by strict Login policy.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        // Extract device info from User-Agent header for session tracking
        var deviceInfo = Request.Headers.UserAgent.ToString();
        
        var result = await _authService.LoginAsync(dto, deviceInfo);

        if (!result.Status)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Exchanges a valid Refresh Token for a new Access Token (JWT rotation).
    /// Rate limited by General policy.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.General)]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);

        if (!result.Status)
        {
            return UnprocessableEntity(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Revokes the provided refresh token and ends the current session.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitPolicies.General)]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.LogoutAsync(dto.RefreshToken);
        return Ok(result);
    }

    /// <summary>
    /// Revokes ALL active sessions and invalidates existing JWTs by rotating the SecurityStamp.
    /// </summary>
    [Authorize]
    [HttpPost("logout-all")]
    [EnableRateLimiting(RateLimitPolicies.General)]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> LogoutAll()
    {
        // Get the current User ID from the authenticated JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(ApiResponse<MessageResponse>.Failure("Invalid authentication context."));
        }

        var result = await _authService.LogoutAllDevicesAsync(userId);
        return Ok(result);
    }
}
