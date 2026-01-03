using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PagedResponse<NotificationDto>>> GetMyNotificationsAsync(Guid userId, PaginationFilter filter, bool unreadOnly)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                Data = n.Data,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<NotificationDto>>.Success(
            new PagedResponse<NotificationDto>(items, filter.PageNumber, filter.PageSize, total));
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId)
    {
        var count = await _context.Notifications.CountAsync(n => n.RecipientId == userId && !n.IsRead);
        return ApiResponse<int>.Success(count);
    }

    public async Task<ApiResponse<MessageResponse>> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

        if (notification == null) return ApiResponse<MessageResponse>.Failure("Notification not found.");

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Marked as read."));
    }

    public async Task<ApiResponse<MessageResponse>> MarkAllAsReadAsync(Guid userId)
    {
        await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("All notifications marked as read."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteNotificationAsync(Guid userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId);

        if (notification == null) return ApiResponse<MessageResponse>.Failure("Notification not found.");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Notification deleted."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteAllReadAsync(Guid userId)
    {
        await _context.Notifications
            .Where(n => n.RecipientId == userId && n.IsRead)
            .ExecuteDeleteAsync();

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Read notifications cleared."));
    }

    public async Task CreateNotificationAsync(Guid recipientId, string title, string body, NotificationType type, string? data = null)
    {
        var notification = new Notification
        {
            RecipientId = recipientId,
            Title = title,
            Body = body,
            Type = type,
            Data = data,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
