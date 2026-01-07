using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class SystemStatusDto
{
    // Account Statistics (Based on Account entities)
    public int TotalAccountCount { get; set; }
    public Dictionary<AccountType, int> AccountsByType { get; set; } = [];
    public Dictionary<AccountRole, int> AccountsByRole { get; set; } = [];
    public Dictionary<AccountStatus, int> AccountsByStatus { get; set; } = [];

    // Token Statistics
    public int TotalRefreshTokenCount { get; set; }
    public int ActiveRefreshTokenCount { get; set; }
    public int ExpiredRefreshTokenCount { get; set; }

    // Profile Statistics
    public int TotalOrganizationCount { get; set; }
    public int TotalPersonalProfileCount { get; set; }

    // Notification Statistics
    public int TotalNotificationCount { get; set; }

    // Asset Library Statistics
    public int TotalAccountAssetCount { get; set; }
    public Dictionary<string, int> AccountAssetsByVisibility { get; set; } = [];
    public int TotalSystemAssetCount { get; set; }

    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}