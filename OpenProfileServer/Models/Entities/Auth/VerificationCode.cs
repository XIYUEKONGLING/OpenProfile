using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Auth;

public class VerificationCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The target identifier (Email, Phone, or AccountId depending on context).
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Code { get; set; } = string.Empty;

    public VerificationType Type { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}