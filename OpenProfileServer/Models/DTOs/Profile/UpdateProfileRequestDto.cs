using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;

namespace OpenProfileServer.Models.DTOs.Profile;

/// <summary>
/// Used for both POST (Full Update) and PATCH (Partial Update).
/// All fields are nullable to support PATCH behavior.
/// </summary>
public class UpdateProfileRequestDto
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Location { get; set; }
    public string? TimeZone { get; set; }
    public string? Website { get; set; }
    
    public AssetDto? Avatar { get; set; }
    public AssetDto? Background { get; set; }
    
    // Personal
    public string? Pronouns { get; set; }
    public string? JobTitle { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentSchool { get; set; }
    public DateOnly? Birthday { get; set; }
    
    // Organization
    public DateOnly? FoundedDate { get; set; }
}