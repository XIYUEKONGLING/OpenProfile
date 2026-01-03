using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities;

public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RecipientId { get; set; }
    public virtual Account Recipient { get; set; } = null!;

    public NotificationType Type { get; set; } = NotificationType.System;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Url { get; set; }

    /// <summary>
    /// Optional JSON data attached to the notification.
    /// </summary>
    public string? Data { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}