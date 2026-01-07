using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Assets;

public class BatchUpdateVisibilityRequestDto
{
    public required List<Guid> AssetIds { get; set; }
    public Visibility Visibility { get; set; }
}