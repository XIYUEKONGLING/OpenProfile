using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("PersonalSettings")]
public class PersonalSettings : AccountSettings
{
    public Visibility Visibility { get; set; } = Visibility.Public;
    public Visibility DefaultVisibility { get; set; } = Visibility.Public; // Default visibility for newly created lists/items.

}