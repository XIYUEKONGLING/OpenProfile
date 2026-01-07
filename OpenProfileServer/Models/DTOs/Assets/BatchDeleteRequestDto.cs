namespace OpenProfileServer.Models.DTOs.Assets;

public class BatchDeleteRequestDto
{
    public required List<Guid> AssetIds { get; set; }
}