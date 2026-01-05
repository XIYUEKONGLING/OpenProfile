using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;

namespace OpenProfileServer.Models.Entities.Settings;

[Table("SystemSettings")]
public class SystemSettings : AccountSettings
{
    
}