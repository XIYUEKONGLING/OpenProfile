using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Core;

public class AssetDto
{
    public AssetType Type { get; set; } = AssetType.Empty;
    public string? Value { get; set; }
    public string? Tag { get; set; }
}