using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities.Auth;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Token { get; set; } = string.Empty;

    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [MaxLength(256)]
    public string? DeviceInfo { get; set; }
}