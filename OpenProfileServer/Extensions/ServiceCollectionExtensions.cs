using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Configuration.RateLimiting;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.NewtonsoftJson;

namespace OpenProfileServer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all configuration sections and their validators.
    /// </summary>
    public static IServiceCollection AddServerConfiguration(this IServiceCollection services, IConfiguration config)
    {
        // 1. Bind Options
        services.Configure<DatabaseSettings>(config.GetSection(DatabaseSettings.SectionName));
        services.Configure<CacheOptions>(config.GetSection(CacheOptions.SectionName));
        services.Configure<SecurityOptions>(config.GetSection(SecurityOptions.SectionName));
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<RateLimitOptions>(config.GetSection(RateLimitOptions.SectionName));

        // 2. Register Validators
        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        services.AddSingleton<IValidateOptions<CacheOptions>, CacheOptionsValidator>();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Configures FusionCache and MemoryCache using values from CacheOptions.
    /// </summary>
    public static IServiceCollection AddServerCaching(this IServiceCollection services, IConfiguration config)
    {
        // Retrieve options immediately to configure the builder
        var options = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (!options.IsEnabled)
        {
            // Register a disabled/pass-through cache if needed, or do nothing.
            services.AddFusionCache().WithDefaultEntryOptions(new FusionCacheEntryOptions { Duration = TimeSpan.Zero });
            return services;
        }

        // 1. Configure underlying Memory Cache
        services.AddMemoryCache(memOpts =>
        {
            memOpts.SizeLimit = options.SizeLimit;
        });

        // 2. Configure FusionCache
        var fusionBuilder = services.AddFusionCache()
            .WithOptions(fOpts =>
            {
                // Dynamic Circuit Breaker
                fOpts.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(options.DistributedCacheCircuitBreakerSeconds);
            })
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(options.DefaultExpirationMinutes),
                
                IsFailSafeEnabled = options.EnableFailSafe,
                FailSafeMaxDuration = TimeSpan.FromMinutes(options.FailSafeMaxDurationMinutes),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(options.FailSafeThrottleDurationSeconds),
                
                FactorySoftTimeout = TimeSpan.FromMilliseconds(options.FactorySoftTimeoutMilliseconds)
            })
            .WithSerializer(new FusionCacheNewtonsoftJsonSerializer());

        // 3. Configure Distributed Cache (Redis) if enabled
        if (options.UseRedis && !string.IsNullOrWhiteSpace(options.RedisConnection))
        {
            services.AddStackExchangeRedisCache(redisOpts =>
            {
                redisOpts.Configuration = options.RedisConnection;
                redisOpts.InstanceName = options.InstancePrefix;
            });

            fusionBuilder.WithRegisteredDistributedCache();
            
            // Backplane for synchronization
            fusionBuilder.WithStackExchangeRedisBackplane(redisOpts =>
            {
                redisOpts.Configuration = options.RedisConnection;
            });
        }

        return services;
    }

    /// <summary>
    /// Configures Rate Limiting based on defined policies.
    /// </summary>
    public static IServiceCollection AddServerRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new { Message = "Too many requests" }, token);
            };

            foreach (var policy in options.Policies)
            {
                switch (policy.Type)
                {
                    case PolicyType.FixedWindow:
                        limiterOptions.AddFixedWindowLimiter(policy.Name, p =>
                        {
                            p.PermitLimit = policy.PermitLimit;
                            p.Window = TimeSpan.FromSeconds(policy.PeriodSeconds);
                            p.QueueLimit = policy.QueueLimit;
                        });
                        break;
                    case PolicyType.SlidingWindow:
                        limiterOptions.AddSlidingWindowLimiter(policy.Name, p =>
                        {
                            p.PermitLimit = policy.PermitLimit;
                            p.Window = TimeSpan.FromSeconds(policy.PeriodSeconds);
                            p.SegmentsPerWindow = policy.SegmentsPerWindow;
                            p.QueueLimit = policy.QueueLimit;
                        });
                        break;
                }
            }
        });

        return services;
    }
}
