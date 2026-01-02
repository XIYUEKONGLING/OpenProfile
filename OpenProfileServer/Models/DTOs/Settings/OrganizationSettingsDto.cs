using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Settings;

public class OrganizationSettingsDto
{
    public bool AllowFollowers { get; set; }
    public bool ShowFollowingList { get; set; }
    public bool ShowFollowersList { get; set; }
    
    public Visibility Visibility { get; set; }
    public Visibility DefaultVisibility { get; set; }
    public Visibility DefaultMemberVisibility { get; set; }
    public bool AllowMemberInvite { get; set; }
}