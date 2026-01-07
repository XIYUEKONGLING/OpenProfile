using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Assets;

public class CreateSystemAssetRequestDto
{
    [MaxLength(128)]
    public string? Category { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    [Required]
    public AssetDto Asset { get; set; } = new();

    public Visibility Visibility { get; set; } = Visibility.Public;
}
