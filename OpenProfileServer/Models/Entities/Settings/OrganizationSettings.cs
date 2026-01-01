using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("OrganizationSettings")]
public class OrganizationSettings : AccountSettings
{
    /// <summary>
    /// Controls if the Organization page is visible to the public or just members.
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Public;
    /// <summary>
    /// Default visibility for newly created repositories/projects.
    /// </summary>
    public Visibility DefaultVisibility { get; set; } = Visibility.Public; 
    
    public Visibility DefaultMemberVisibility { get; set; } = Visibility.Private;
    
    /// <summary>
    /// If true, current members can invite others without admin approval.
    /// </summary>
    public bool AllowMemberInvite { get; set; } = false;
    
}