using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class CreateNotificationRequestDto
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Administrator;

    [MaxLength(1024)]
    public string? Url { get; set; }
    
    /// <summary>
    /// Optional JSON data payload.
    /// </summary>
    public string? Data { get; set; }
}
