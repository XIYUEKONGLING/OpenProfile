using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities.Auth;

public class AccountEmail
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(128)]
    public string Email { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = false;

    public bool IsVerified { get; set; } = false;

    public DateTime? VerifiedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}