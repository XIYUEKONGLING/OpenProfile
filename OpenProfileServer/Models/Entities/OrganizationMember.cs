using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities;

public class OrganizationMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }
    public virtual OrganizationProfile Organization { get; set; } = null!;

    [Required]
    public Guid AccountId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public MemberRole Role { get; set; } = MemberRole.Member;

    public Visibility Visibility { get; set; } = Visibility.Public;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}