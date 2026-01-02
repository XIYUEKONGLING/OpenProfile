using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Settings;

public class PersonalSettingsDto
{
    public bool AllowFollowers { get; set; }
    public bool ShowFollowingList { get; set; }
    public bool ShowFollowersList { get; set; }
    
    public Visibility Visibility { get; set; }
    public Visibility DefaultVisibility { get; set; }
    public bool ShowLocalTime { get; set; }
}