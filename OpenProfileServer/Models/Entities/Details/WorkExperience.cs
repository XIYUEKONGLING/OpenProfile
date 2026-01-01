using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class WorkExperience
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PersonalProfileId { get; set; }
    public virtual PersonalProfile PersonalProfile { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Position { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; } // Null = Present

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Embed asset for company logo
    public Asset Logo { get; set; } = new();
}