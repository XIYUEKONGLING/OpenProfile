using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateWorkExperienceRequestDto
{
    [Required]
    [MaxLength(128)]
    public string CompanyName { get; set; } = string.Empty;
    
    [MaxLength(128)]
    public string? Position { get; set; }
    
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public AssetDto? Logo { get; set; }
}
