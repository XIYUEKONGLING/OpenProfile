using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class WorkExperienceDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public AssetDto Logo { get; set; } = new();
}