using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateEducationExperienceRequestDto
{
    [Required]
    [MaxLength(128)]
    public string SchoolName { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string? Degree { get; set; }
    
    [MaxLength(128)]
    public string? Major { get; set; }
    
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    
    public AssetDto? Logo { get; set; }
}