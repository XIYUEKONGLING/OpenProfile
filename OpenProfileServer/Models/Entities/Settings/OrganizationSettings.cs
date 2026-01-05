using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("OrganizationSettings")]
public class OrganizationSettings : AccountSettings
{
    public Visibility DefaultMemberVisibility { get; set; } = Visibility.Private;
    
    /// <summary>
    /// If true, current members can invite others without admin approval.
    /// </summary>
    public bool AllowMemberInvite { get; set; } = false;
    
}