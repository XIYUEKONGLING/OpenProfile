using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Core;

/// <summary>
/// DTO for public assets (no authentication required).
/// Supports both AccountAsset and SystemAsset.
/// AccountId is nullable to support system assets which have no owner.
/// </summary>
public class PublicAssetDto
{
    public Guid Id { get; set; }
    public Guid? AccountId { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public AssetDto Asset { get; set; } = new();
    public Visibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
