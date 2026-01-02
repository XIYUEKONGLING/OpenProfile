using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Organization;

public class CreateOrganizationRequestDto
{
    [Required]
    [MaxLength(64)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;
    
    public string? Description { get; set; }
}