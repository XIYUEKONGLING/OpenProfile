using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class OrganizationPermissionsDto
{
    /// <summary>
    /// Indicates if the current member can invite others to the organization.
    /// This is true for Owner/Admin, or for Members when AllowMemberInvite setting is enabled.
    /// </summary>
    public bool CanInvite { get; set; }

    /// <summary>
    /// The current member's role in the organization.
    /// </summary>
    public MemberRole Role { get; set; }
}
