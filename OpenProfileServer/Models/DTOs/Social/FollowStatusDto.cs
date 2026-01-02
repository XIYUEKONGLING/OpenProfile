namespace OpenProfileServer.Models.DTOs.Social;

public class FollowStatusDto
{
    public bool IsFollowing { get; set; }
    public bool IsFollowedBy { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsBlockedBy { get; set; }
}