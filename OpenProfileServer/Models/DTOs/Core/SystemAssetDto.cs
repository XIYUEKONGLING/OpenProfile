using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Core;

public class SystemAssetDto
{
    public Guid Id { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public AssetDto Asset { get; set; } = new();
    public Visibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
