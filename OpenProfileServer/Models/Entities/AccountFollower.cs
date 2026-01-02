using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities;

/// <summary>
/// Represents a follow relationship between two accounts.
/// </summary>
public class AccountFollower
{
    [Required]
    public Guid FollowerId { get; set; }
    public virtual Account Follower { get; set; } = null!;

    [Required]
    public Guid FollowingId { get; set; }
    public virtual Account Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}