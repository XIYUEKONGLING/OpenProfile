using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities.Auth;

/// <summary>
/// Sensitive login credentials. 
/// ONLY exists for Personal accounts.
/// </summary>
public class AccountCredential
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PasswordSalt { get; set; } = string.Empty;

    
    public bool IsTwoFactorEnabled { get; set; } = false;
    
    [MaxLength(128)]
    public string? TotpSecret { get; set; }
    
    /// <summary>
    /// JSON array or hashed string of backup codes.
    /// </summary>
    public string? BackupCodes { get; set; }
    
    /// <summary>
    /// For "Sign out from all devices" functionality.
    /// </summary>
    [MaxLength(128)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}