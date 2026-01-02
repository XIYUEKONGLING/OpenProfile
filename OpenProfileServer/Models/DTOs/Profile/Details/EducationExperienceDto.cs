using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class EducationExperienceDto
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Major { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public AssetDto Logo { get; set; } = new();
}
