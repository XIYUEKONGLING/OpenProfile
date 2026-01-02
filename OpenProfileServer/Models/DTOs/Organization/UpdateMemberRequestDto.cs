using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

/// <summary>
/// Used for updating a member's role (by Owner) or self-details (by Member).
/// </summary>
public class UpdateMemberRequestDto
{
    public MemberRole? Role { get; set; }
    public string? Title { get; set; }
    public Visibility? Visibility { get; set; }
}