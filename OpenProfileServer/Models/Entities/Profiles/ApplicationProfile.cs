using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;

namespace OpenProfileServer.Models.Entities.Profiles;

[Table("ApplicationProfiles")]
public class ApplicationProfile : Profile
{
    public Guid? DeveloperAccountId { get; set; } 

    [MaxLength(512)]
    public string? TermsOfServiceUrl { get; set; }
}