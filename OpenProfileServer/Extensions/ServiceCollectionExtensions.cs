using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Configuration.RateLimiting;
using OpenProfileServer.Constants;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.NewtonsoftJson;

namespace OpenProfileServer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all configuration sections and their corresponding validators.
    /// </summary>
    public static IServiceCollection AddServerConfiguration(this IServiceCollection services, IConfiguration config)
    {
        // Bind Options Sections
        services.Configure<DatabaseSettings>(config.GetSection(DatabaseSettings.SectionName));
        services.Configure<CacheOptions>(config.GetSection(CacheOptions.SectionName));
        services.Configure<SecurityOptions>(config.GetSection(SecurityOptions.SectionName));
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<RateLimitOptions>(config.GetSection(RateLimitOptions.SectionName));

        // Register Validators
        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        services.AddSingleton<IValidateOptions<CacheOptions>, CacheOptionsValidator>();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Configures FusionCache and MemoryCache using dynamic values from CacheOptions.
    /// No parameters are hardcoded.
    /// </summary>
    public static IServiceCollection AddServerCaching(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (!options.IsEnabled)
        {
            // Register a dummy cache with zero duration to satisfy DI requirements
            services.AddFusionCache().WithDefaultEntryOptions(new FusionCacheEntryOptions { Duration = TimeSpan.Zero });
            return services;
        }

        // 1. Configure Local Memory Cache
        services.AddMemoryCache(memOpts =>
        {
            memOpts.SizeLimit = options.SizeLimit;
        });

        // 2. Configure FusionCache
        var fusionBuilder = services.AddFusionCache()
            .WithOptions(fOpts =>
            {
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

        // 3. Configure Redis (L2) and Backplane if connection is provided
        if (options.UseRedis && !string.IsNullOrWhiteSpace(options.RedisConnection))
        {
            services.AddStackExchangeRedisCache(redisOpts =>
            {
                redisOpts.Configuration = options.RedisConnection;
                redisOpts.InstanceName = options.InstancePrefix;
            });

            fusionBuilder.WithRegisteredDistributedCache();
            
            fusionBuilder.WithStackExchangeRedisBackplane(redisOpts =>
            {
                redisOpts.Configuration = options.RedisConnection;
            });
        }

        return services;
    }

    /// <summary>
    /// Configures Rate Limiting with dynamic policies from config and hardcoded secure defaults.
    /// </summary>
    public static IServiceCollection AddServerRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            var registeredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Register User-Defined Policies from appsettings.json
            foreach (var policy in options.Policies)
            {
                RegisterPolicy(limiterOptions, policy);
                registeredNames.Add(policy.Name);
            }

            // Register Default Security Policies if not already defined in config

            {
                // General: 100 requests per minute (Sliding window for smoothness)
                if (!registeredNames.Contains(RateLimitPolicies.General))
                {
                    limiterOptions.AddSlidingWindowLimiter(RateLimitPolicies.General, opt =>
                    {
                        opt.PermitLimit = 100;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.SegmentsPerWindow = 5;
                        opt.QueueLimit = 10;
                    });
                }

                // Login: 5 attempts per minute (Strict)
                if (!registeredNames.Contains(RateLimitPolicies.Login))
                {
                    limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Login, opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });
                }

                // Register: 3 accounts per hour
                if (!registeredNames.Contains(RateLimitPolicies.Register))
                {
                    limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Register, opt =>
                    {
                        opt.PermitLimit = 3;
                        opt.Window = TimeSpan.FromHours(1);
                        opt.QueueLimit = 0;
                    });
                }

                // Email: 2 actions per minute (Reset password, verification code)
                if (!registeredNames.Contains(RateLimitPolicies.Email))
                {
                    limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Email, opt =>
                    {
                        opt.PermitLimit = 2;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });
                }
            }

            // Global Rejection Logic
            limiterOptions.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                await context.HttpContext.Response.WriteAsJsonAsync(new 
                { 
                    Message = "Too many requests. Please try again later." 
                }, token);
            };
        });

        return services;
    }

    /// <summary>
    /// Helper to register a policy based on its type.
    /// </summary>
    private static void RegisterPolicy(RateLimiterOptions limiterOptions, RateLimitPolicy policy)
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
            case PolicyType.TokenBucket:
                limiterOptions.AddTokenBucketLimiter(policy.Name, p =>
                {
                    p.TokenLimit = policy.PermitLimit;
                    p.ReplenishmentPeriod = TimeSpan.FromSeconds(policy.PeriodSeconds);
                    p.TokensPerPeriod = policy.PermitLimit / 2 > 0 ? policy.PermitLimit / 2 : 1;
                    p.QueueLimit = policy.QueueLimit;
                });
                break;
        }
    }
}
