using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class EducationExperience
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PersonalProfileId { get; set; }
    public virtual PersonalProfile PersonalProfile { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Degree { get; set; }

    [MaxLength(128)]
    public string? Major { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // Embed asset for school logo
    public Asset Logo { get; set; } = new();
}