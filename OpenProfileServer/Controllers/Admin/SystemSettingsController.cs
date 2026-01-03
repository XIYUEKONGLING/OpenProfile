using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Admin;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.Admin;

/// <summary>
/// Endpoints for managing runtime configuration (e.g., Toggle Registration, Maintenance Mode).
/// Requires Admin or Higher privileges.
/// </summary>
[Authorize(Roles = AccountRoles.AdminOrHigher)]
[Route("api/admin/system-settings")]
[ApiController]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// GET /api/admin/system-settings
    /// List all configurable system settings.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingDto>>>> GetAll()
    {
        var settings = await _settingService.GetAllSettingsAsync();
        return Ok(ApiResponse<IEnumerable<SystemSettingDto>>.Success(settings));
    }

    /// <summary>
    /// GET /api/admin/system-settings/{key}
    /// Get a specific setting value.
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<SystemSettingDto>>> GetOne(string key)
    {
        // We reuse the list fetch or individual fetch. 
        // For logic consistency, we fetch from DB/Cache specifically.
        var val = await _settingService.GetValueAsync(key);
        if (val == null) return NotFound(ApiResponse<SystemSettingDto>.Failure("Setting key not found."));
        
        // Retrieve full object to get metadata like Description/Type
        // Optimization: In a real app we might add GetEntityAsync to the service, 
        // here we simply reconstruct or fetch the list and find it.
        var all = await _settingService.GetAllSettingsAsync();
        var dto = all.FirstOrDefault(s => s.Key == key);
        
        return dto != null 
            ? Ok(ApiResponse<SystemSettingDto>.Success(dto)) 
            : NotFound(ApiResponse<SystemSettingDto>.Failure("Setting not found."));
    }

    /// <summary>
    /// PUT /api/admin/system-settings/{key}
    /// Update a specific system setting.
    /// </summary>
    [HttpPut("{key}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Update(string key, [FromBody] UpdateSystemSettingRequestDto dto)
    {
        // 1. Verify key existence to prevent polluting DB with garbage keys via API
        var existing = await _settingService.GetValueAsync(key);
        if (existing == null)
        {
            return NotFound(ApiResponse<MessageResponse>.Failure($"Setting '{key}' does not exist."));
        }

        // 2. Update
        await _settingService.SetValueAsync(key, dto.Value);

        return Ok(ApiResponse<MessageResponse>.Success(MessageResponse.Create($"Setting '{key}' updated successfully.")));
    }
}
