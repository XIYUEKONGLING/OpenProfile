namespace OpenProfileServer.Models.DTOs.Profile;

public class ProfilePrivacyDto
{
    public bool ShowFollowers { get; set; } = true;
    public bool ShowFollowing { get; set; } = true;
}