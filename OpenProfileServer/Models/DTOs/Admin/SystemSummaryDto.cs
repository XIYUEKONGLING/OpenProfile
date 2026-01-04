using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class SystemStatusDto
{
    public int TotalAccountCount { get; set; }
    public Dictionary<AccountType, int> AccountsByType { get; set; } = [];
    public Dictionary<AccountRole, int> AccountsByRole { get; set; } = [];
    public Dictionary<AccountStatus, int> AccountsByStatus { get; set; } = [];

    public int TotalRefreshTokenCount { get; set; }
    public int ActiveRefreshTokenCount { get; set; }
    public int ExpiredRefreshTokenCount { get; set; }

    public int TotalOrganizationCount { get; set; }
    public int TotalPersonalProfileCount { get; set; }
    public int TotalNotificationCount { get; set; }
    
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}