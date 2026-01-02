namespace OpenProfileServer.Models.Enums;

public enum AccountStatus
{
    /// <summary>
    /// Account is active and functioning normally.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Account is marked for deletion. It enters a cooling-off period 
    /// where the owner can recover it, and the AccountName is not yet available for reuse.
    /// </summary>
    PendingDeletion = 1,

    /// <summary>
    /// Account is permanently banned due to policy violations.
    /// </summary>
    Banned = 2,

    /// <summary>
    /// Account is temporarily suspended (e.g., suspicious activity, pending investigation).
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// Account is deactivated by the user (Hidden but not deleted).
    /// </summary>
    Deactivated = 4
}