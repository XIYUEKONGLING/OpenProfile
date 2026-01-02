namespace OpenProfileServer.Configuration.RateLimiting;

public enum PolicyType
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
    Concurrency
}