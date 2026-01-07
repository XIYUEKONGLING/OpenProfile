using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Controllers.Organization;

/// <summary>
/// Organization Notification Management (/api/orgs/{org}/notifications).
/// Requires authentication and organization Owner/Admin access.
/// </summary>
[Authorize]
[Route("api/orgs/{org}/notifications")]
[ApiController]
public class OrganizationNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IProfileService _profileService;
    private readonly IOrganizationService _orgService;

    public OrganizationNotificationsController(
        INotificationService notificationService,
        IProfileService profileService,
        IOrganizationService orgService)
    {
        _notificationService = notificationService;
        _profileService = profileService;
        _orgService = orgService;
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
    /// Helper to check if current user is Owner or Admin of the org.
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

        // Only Owner and Admin can access notifications
        if (roleResult.Status && (roleResult.Data.Role == MemberRole.Owner || roleResult.Data.Role == MemberRole.Admin))
        {
            return orgId;
        }
        return null;
    }

    /// <summary>
    /// GET /api/orgs/{org}/notifications
    /// List organization notifications (paginated). Owner/Admin access required.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<NotificationDto>>>> GetList(
        string org,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        var filter = new PaginationFilter { PageNumber = page, PageSize = pageSize };
        return Ok(await _notificationService.GetMyNotificationsAsync(orgId.Value, filter, unreadOnly));
    }

    /// <summary>
    /// GET /api/orgs/{org}/notifications/unread-count
    /// Get unread notification count. Owner/Admin access required.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(string org)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _notificationService.GetUnreadCountAsync(orgId.Value));
    }

    /// <summary>
    /// PATCH /api/orgs/{org}/notifications/{id}/read
    /// Mark notification as read. Owner/Admin access required.
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> MarkRead(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _notificationService.MarkAsReadAsync(orgId.Value, id));
    }

    /// <summary>
    /// POST /api/orgs/{org}/notifications/read-all
    /// Mark all notifications as read. Owner/Admin access required.
    /// </summary>
    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> MarkAllRead(string org)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _notificationService.MarkAllAsReadAsync(orgId.Value));
    }

    /// <summary>
    /// DELETE /api/orgs/{org}/notifications/{id}
    /// Delete notification. Owner/Admin access required.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Delete(string org, Guid id)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _notificationService.DeleteNotificationAsync(orgId.Value, id));
    }

    /// <summary>
    /// DELETE /api/orgs/{org}/notifications
    /// Delete all read notifications. Owner/Admin access required.
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteAllRead(string org)
    {
        var orgId = await CheckManagePermission(org);
        if (orgId == null)
            return NotFound(ApiResponse<MessageResponse>.Failure("Organization not found or access denied."));

        return Ok(await _notificationService.DeleteAllReadAsync(orgId.Value));
    }
}
