using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Url { get; set; }
    public AssetDto Logo { get; set; } = new();
    public int DisplayOrder { get; set; }
    public Visibility Visibility { get; set; }
}