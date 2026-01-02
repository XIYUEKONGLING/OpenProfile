namespace OpenProfileServer.Configuration.RateLimiting;

public class RateLimitPolicy
{
    public string Name { get; set; } = string.Empty;
    public PolicyType Type { get; set; } = PolicyType.FixedWindow;
    public int PermitLimit { get; set; }
    public int PeriodSeconds { get; set; }
    public int QueueLimit { get; set; }
    
    // For sliding window
    public int SegmentsPerWindow { get; set; } = 8;
}