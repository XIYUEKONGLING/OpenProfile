using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Entities.Details;

namespace OpenProfileServer.Models.Entities.Profiles;

[Table("PersonalProfiles")]
public class PersonalProfile : Profile
{
    [MaxLength(32)]
    public string? Pronouns { get; set; } // e.g., "he/him"

    public DateOnly? Birthday { get; set; }
    
    [MaxLength(128)]
    public string? JobTitle { get; set; }

    // Collections specific to humans
    public virtual ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public virtual ICollection<EducationExperience> EducationExperiences { get; set; } = new List<EducationExperience>();
}