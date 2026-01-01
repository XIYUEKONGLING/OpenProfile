namespace OpenProfileServer.Models.Enums;

/// <summary>
/// Defines the role of an account within an organization.
/// </summary>
public enum MemberRole
{
    /// <summary>
    /// Regular member with standard permissions.
    /// </summary>
    Member = 0,

    /// <summary>
    /// Can manage content and other members.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// The creator or highest authority of the organization.
    /// </summary>
    Owner = 2,

    /// <summary>
    /// Limited access, typically for external collaborators.
    /// </summary>
    Guest = 3,
}