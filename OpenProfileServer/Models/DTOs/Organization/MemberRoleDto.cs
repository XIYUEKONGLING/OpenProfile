using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class MemberRoleDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public MemberRole Role { get; set; }
    public string? Title { get; set; }
    public Visibility Visibility { get; set; }
}