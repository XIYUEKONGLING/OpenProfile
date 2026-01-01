using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.ValueObjects;

[Owned]
public class Asset
{
    public AssetType Type { get; set; } = AssetType.Text;

    public string? Value { get; set; }

    [MaxLength(512)]
    public string? Tag { get; set; }
}