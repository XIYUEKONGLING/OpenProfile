using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;

namespace OpenProfileServer.Controllers.User;

[Authorize]
[Route("api/me/notifications")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim != null && Guid.TryParse(idClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<NotificationDto>>>> GetList(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        [FromQuery] bool unreadOnly = false)
    {
        var filter = new PaginationFilter { PageNumber = page, PageSize = pageSize };
        return Ok(await _notificationService.GetMyNotificationsAsync(GetUserId(), filter, unreadOnly));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        return Ok(await _notificationService.GetUnreadCountAsync(GetUserId()));
    }

    [HttpPatch("{id}/read")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> MarkRead(Guid id)
    {
        return Ok(await _notificationService.MarkAsReadAsync(GetUserId(), id));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> MarkAllRead()
    {
        return Ok(await _notificationService.MarkAllAsReadAsync(GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> Delete(Guid id)
    {
        return Ok(await _notificationService.DeleteNotificationAsync(GetUserId(), id));
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<MessageResponse>>> DeleteAllRead()
    {
        return Ok(await _notificationService.DeleteAllReadAsync(GetUserId()));
    }
}
