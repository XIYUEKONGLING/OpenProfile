using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateSponsorshipItemRequestDto
{
    [Required]
    [MaxLength(64)]
    public string Platform { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Url { get; set; }

    public AssetDto? Icon { get; set; }
    public AssetDto? QrCode { get; set; }
    public int? DisplayOrder { get; set; }
    public Visibility? Visibility { get; set; }
}