using OpenProfileServer.Configuration.RateLimiting;

namespace OpenProfileServer.Configuration;

public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";
    public List<RateLimitPolicy> Policies { get; set; } = new();
}