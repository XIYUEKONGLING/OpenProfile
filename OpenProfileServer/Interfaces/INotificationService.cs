using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;

namespace OpenProfileServer.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<PagedResponse<NotificationDto>>> GetMyNotificationsAsync(Guid userId, PaginationFilter filter, bool unreadOnly);
    Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId);
    Task<ApiResponse<MessageResponse>> MarkAsReadAsync(Guid userId, Guid notificationId);
    Task<ApiResponse<MessageResponse>> MarkAllAsReadAsync(Guid userId);
    Task<ApiResponse<MessageResponse>> DeleteNotificationAsync(Guid userId, Guid notificationId);
    Task<ApiResponse<MessageResponse>> DeleteAllReadAsync(Guid userId);
    
    // Internal helper to send notification
    Task CreateNotificationAsync(Guid recipientId, string title, string body, Models.Enums.NotificationType type, string? data = null);
}