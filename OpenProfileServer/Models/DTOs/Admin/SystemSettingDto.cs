namespace OpenProfileServer.Models.DTOs.Admin;

public class SystemSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// Hint for the frontend UI: "boolean", "number", "string", "json", "html".
    /// </summary>
    public string? ValueType { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}