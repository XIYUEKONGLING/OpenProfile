using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class OrganizationInvitationDto
{
    public Guid Id { get; set; }
    
    // Org Info
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public AssetDto OrganizationAvatar { get; set; } = new();

    // Inviter Info (Who sent it)
    public Guid InviterId { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public AssetDto InviterAvatar { get; set; } = new();

    // Invitee Info (Who received it - relevant for admin view)
    public Guid InviteeId { get; set; }
    public string InviteeName { get; set; } = string.Empty;
    public AssetDto InviteeAvatar { get; set; } = new();

    public MemberRole Role { get; set; }
    public string? Title { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}