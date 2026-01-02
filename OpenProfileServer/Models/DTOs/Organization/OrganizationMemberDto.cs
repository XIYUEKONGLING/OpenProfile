using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class OrganizationMemberDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AssetDto Avatar { get; set; } = new();
    
    public MemberRole Role { get; set; }
    public string? Title { get; set; }
    public Visibility Visibility { get; set; }
    public DateTime JoinedAt { get; set; }
}