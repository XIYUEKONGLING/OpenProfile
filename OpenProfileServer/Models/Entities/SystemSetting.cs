using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.Entities;

public class SystemSetting
{
    [Key]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional hint for UI rendering (e.g., "boolean", "json", "number").
    /// </summary>
    [MaxLength(64)]
    public string? ValueType { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}