using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities.Auth;

/// <summary>
/// Advanced security configurations (2FA, TOTP, Backup Codes).
/// Separated from basic credentials for better security isolation.
/// </summary>
public class AccountSecurity
{
    /// <summary>
    /// Shared Primary Key with Account.
    /// </summary>
    [Key]
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public bool IsTwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Secret key for TOTP (Authenticator apps).
    /// </summary>
    [MaxLength(128)]
    public string? TotpSecret { get; set; }

    /// <summary>
    /// Collection of hashed/encrypted recovery codes.
    /// </summary>
    public List<string> BackupCodes { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}