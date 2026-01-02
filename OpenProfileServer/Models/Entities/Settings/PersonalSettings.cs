using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("PersonalSettings")]
public class PersonalSettings : AccountSettings
{
    public Visibility Visibility { get; set; } = Visibility.Public;
    public Visibility DefaultVisibility { get; set; } = Visibility.Public; // Default visibility for newly created lists/items.
    
    /// <summary>
    /// Whether to show the user's current local time based on their TimeZone setting.
    /// </summary>
    public bool ShowLocalTime { get; set; } = false;
}