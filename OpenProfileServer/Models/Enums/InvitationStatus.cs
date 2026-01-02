namespace OpenProfileServer.Models.Enums;

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3, // Cancelled by Org Admin
    Expired = 4
}