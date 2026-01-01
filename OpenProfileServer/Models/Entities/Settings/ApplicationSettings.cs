using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("ApplicationSettings")]
public class ApplicationSettings : AccountSettings
{
    [MaxLength(1024)]
    public string? CallbackUrl { get; set; }

    [MaxLength(512)]
    public string? WebhookSecret { get; set; }

    public int RateLimitQuota { get; set; } = 1024;
}