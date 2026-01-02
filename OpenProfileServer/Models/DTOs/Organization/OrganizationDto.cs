using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AssetDto Avatar { get; set; } = new();
    public AccountStatus Status { get; set; }
    
    // Current user's role in this org
    public MemberRole MyRole { get; set; }
}