using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities.Auth;

/// <summary>
/// Basic login credentials (Password).
/// </summary>
public class AccountCredential
{
    [Key]
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// For "Sign out from all devices" functionality.
    /// </summary>
    [MaxLength(128)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}