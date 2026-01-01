using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities;

public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required] [MaxLength(128)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)] public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(256)] public string PasswordSalt { get; set; } = string.Empty;

    public AccountType Type { get; set; }
    public AccountRole Role { get; set; } = AccountRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    
    public virtual Profile? Profile { get; set; }
    public virtual AccountSettings? Settings { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<OrganizationMember> Memberships { get; set; } = new List<OrganizationMember>();
    
    // [MaxLength(64)]
    // public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
}