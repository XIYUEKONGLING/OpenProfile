using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Assets;

public class UpdateAccountAssetRequestDto
{
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public AssetDto? Asset { get; set; }
    public Visibility? Visibility { get; set; }
}