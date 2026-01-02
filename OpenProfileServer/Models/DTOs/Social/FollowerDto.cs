using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Social;

public class FollowerDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AssetDto Avatar { get; set; } = new();
    public string? Description { get; set; }
}