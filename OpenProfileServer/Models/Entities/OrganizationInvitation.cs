using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities;

public class OrganizationInvitation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }
    public virtual OrganizationProfile Organization { get; set; } = null!;

    /// <summary>
    /// The admin who sent the invitation.
    /// </summary>
    [Required]
    public Guid InviterId { get; set; }
    public virtual Account Inviter { get; set; } = null!;

    /// <summary>
    /// The user being invited.
    /// </summary>
    [Required]
    public Guid InviteeId { get; set; }
    public virtual Account Invitee { get; set; } = null!;

    public MemberRole Role { get; set; } = MemberRole.Member;
    
    [MaxLength(128)]
    public string? Title { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}