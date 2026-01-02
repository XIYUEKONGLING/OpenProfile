namespace OpenProfileServer.Configuration;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public bool IsEnabled { get; set; } = true;

    // Redis Configuration
    public bool UseRedis { get; set; } = false;
    public string? RedisConnection { get; set; }
    public string InstancePrefix { get; set; } = "OpenProfile:";

    // Memory / General Configuration
    public int DefaultExpirationMinutes { get; set; } = 30;
    
    public long SizeLimit { get; set; } = 1024;

    // Resilience / FusionCache Specifics
    public bool EnableFailSafe { get; set; } = true;
    public int FailSafeMaxDurationMinutes { get; set; } = 120;
    public int FailSafeThrottleDurationSeconds { get; set; } = 30;
    
    public int FactorySoftTimeoutMilliseconds { get; set; } = 500;
    public int DistributedCacheCircuitBreakerSeconds { get; set; } = 2;
}