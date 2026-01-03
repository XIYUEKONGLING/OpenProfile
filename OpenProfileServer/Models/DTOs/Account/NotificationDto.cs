using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Account;

public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; } // JSON format string
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}