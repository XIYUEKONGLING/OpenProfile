using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Core;

public class AssetDto
{
    public AssetType Type { get; set; } = AssetType.Text;
    public string? Value { get; set; }
    public string? Tag { get; set; }
}