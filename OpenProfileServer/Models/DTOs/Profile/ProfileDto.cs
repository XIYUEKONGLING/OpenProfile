using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile;

/// <summary>
/// The unified DTO for displaying a profile (Personal or Organization).
/// Fields may be null based on AccountStatus (Suspended/Banned).
/// </summary>
public class ProfileDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountStatus Status { get; set; }
    
    // Basic Info (Available for Active & Suspended)
    public string? DisplayName { get; set; }
    public AssetDto? Avatar { get; set; }
    public AssetDto? Background { get; set; }
    public string? Pronouns { get; set; }

    // Detailed Info (Null if Suspended/Banned)
    public string? Description { get; set; }
    public string? Content { get; set; } // Markdown
    public string? Location { get; set; }
    public string? TimeZone { get; set; }
    public string? Website { get; set; }
    
    // Personal Specific
    public string? JobTitle { get; set; }
    public string? CurrentCompany { get; set; }
    public string? CurrentSchool { get; set; }
    public DateOnly? Birthday { get; set; }

    // Organization Specific
    public DateOnly? FoundedDate { get; set; }
    
    // Social Stats
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}