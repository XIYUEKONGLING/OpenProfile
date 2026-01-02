namespace OpenProfileServer.Models.DTOs.Profile.Details;

using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

public class ContactMethodDto
{
    public Guid Id { get; set; }
    public ContactType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public AssetDto Icon { get; set; } = new();
    public AssetDto Image { get; set; } = new();
    public Visibility Visibility { get; set; }
}