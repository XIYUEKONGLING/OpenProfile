using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile;

public class PublicOrganizationMembershipDto
{
    public Guid OrganizationId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AssetDto Avatar { get; set; } = new();
    
    // The user's role/title within that organization
    public MemberRole Role { get; set; }
    public string? Title { get; set; }
    public DateTime JoinedAt { get; set; }
}