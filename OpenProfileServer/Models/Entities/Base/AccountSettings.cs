using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Base;

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
    
    public Visibility Visibility { get; set; } = Visibility.Public;
    
    //  // Default visibility for newly created lists/items.
    public Visibility DefaultVisibility { get; set; } = Visibility.Public;
    
    /// <summary>
    /// Whether others are allowed to follow this account.
    /// </summary>
    public bool AllowFollowers { get; set; } = true;
    
    /// <summary>
    /// Whether to show the 'Following' list to the public.
    /// </summary>
    public bool ShowFollowingList { get; set; } = true;
    
    /// <summary>
    /// Whether to show the 'Followers' list to the public.
    /// </summary>
    public bool ShowFollowersList { get; set; } = true;
}