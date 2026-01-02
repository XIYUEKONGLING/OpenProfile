using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities;

/// <summary>
/// Represents a block relationship between two accounts.
/// If A blocks B, A is the Blocker and B is the Blocked.
/// </summary>
public class AccountBlock
{
    [Required]
    public Guid BlockerId { get; set; }
    public virtual Account Blocker { get; set; } = null!;

    [Required]
    public Guid BlockedId { get; set; }
    public virtual Account Blocked { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}