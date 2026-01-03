namespace OpenProfileServer.Models.Enums;

public enum NotificationType
{
    /// <summary>
    /// Platform announcements, maintenance alerts, or automated system events.
    /// </summary>
    System = 0,

    /// <summary>
    /// Manual messages sent directly by an administrator to a specific user.
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Account security events (Login alerts, Password changes, 2FA codes).
    /// </summary>
    Security = 2,

    /// <summary>
    /// All interactions involving other users or organizations (Follows, Invites, Mentions, Role updates).
    /// Specific actions are handled via the notification's Data payload.
    /// </summary>
    Interaction = 3
}