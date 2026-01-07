using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Core;

/// <summary>
/// Unified asset DTO for lookup operations across user, organization, and system asset libraries.
/// AccountId is nullable to support system assets (which don't have an owner account).
/// </summary>
public class LookupAssetDto
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
