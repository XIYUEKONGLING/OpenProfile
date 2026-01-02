using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateContactMethodRequestDto
{
    public ContactType? Type { get; set; }
    
    [MaxLength(128)]
    public string? Label { get; set; }

    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    public AssetDto? Icon { get; set; }
    public AssetDto? Image { get; set; }
    public Visibility? Visibility { get; set; }
}