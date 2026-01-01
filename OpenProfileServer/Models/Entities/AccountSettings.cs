using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenProfileServer.Models.Entities;

/// <summary>
/// Abstract base class for PRIVATE account configurations.
/// Strictly separated from Public Profiles.
/// </summary>
[Table("AccountSettings")]
public abstract class AccountSettings
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public virtual Account Account { get; set; } = null!;
}