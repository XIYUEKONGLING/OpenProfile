using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenProfileServer.Configuration;
using OpenProfileServer.Configuration.RateLimiting;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Services;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.NewtonsoftJson;

namespace OpenProfileServer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all configuration sections and their corresponding validators.
    /// This ensures the application fails fast at startup if critical config is missing.
    /// </summary>
    public static IServiceCollection AddServerConfiguration(this IServiceCollection services, IConfiguration config)
    {
        // Bind Options Sections
        services.Configure<ApplicationOptions>(config.GetSection(ApplicationOptions.SectionName));
        services.Configure<DatabaseSettings>(config.GetSection(DatabaseSettings.SectionName));
        services.Configure<CacheOptions>(config.GetSection(CacheOptions.SectionName));
        services.Configure<SecurityOptions>(config.GetSection(SecurityOptions.SectionName));
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<RateLimitOptions>(config.GetSection(RateLimitOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<CompressionOptions>(config.GetSection(CompressionOptions.SectionName));

        // Register Validators to enforce integrity rules
        services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptionsValidator>();
        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        services.AddSingleton<IValidateOptions<CacheOptions>, CacheOptionsValidator>();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddSingleton<IValidateOptions<CompressionOptions>, CompressionOptionsValidator>();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Configures Data Protection for token security. 
    /// Supports both standalone (File System) and distributed (Redis) deployments.
    /// </summary>
    public static IServiceCollection AddServerDataProtection(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        // 1. Initialize Data Protection builder
        var dpBuilder = services.AddDataProtection()
            .SetApplicationName(ApplicationInformation.ServerName); // Ensures all instances share the same key scope

        // 2. Configure persistence based on deployment mode
        if (options.UseRedis && !string.IsNullOrWhiteSpace(options.RedisConnection))
        {
            // Distributed mode: Persist keys to Redis cluster
            var redis = ConnectionMultiplexer.Connect(options.RedisConnection);
            dpBuilder.PersistKeysToStackExchangeRedis(redis, $"{options.InstancePrefix}DataProtectionKeys");
        }
        else
        {
            // Standalone mode: Keys are persisted to the default local path
            // On Linux: ~/.aspnet/DataProtection-Keys
        }

        return services;
    }

    /// <summary>
    /// Configures FusionCache and MemoryCache using dynamic values from CacheOptions.
    /// We use FusionCache to handle the "Cache Stampede" problem and provide a transparent L1+L2 strategy.
    /// </summary>
    public static IServiceCollection AddServerCaching(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (!options.IsEnabled)
        {
            // Register a dummy cache with zero duration to satisfy DI requirements without breaking logic
            services.AddFusionCache().WithDefaultEntryOptions(new FusionCacheEntryOptions { Duration = TimeSpan.Zero });
            return services;
        }

        // 1. Configure Local Memory Cache (L1)
        services.AddMemoryCache(memOpts =>
        {
            memOpts.SizeLimit = options.SizeLimit;
        });

        // 2. Configure FusionCache
        var fusionBuilder = services.AddFusionCache()
            .WithOptions(fOpts =>
            {
                // Break the circuit if the distributed cache is down to prevent cascading failures
                fOpts.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(options.DistributedCacheCircuitBreakerSeconds);
            })
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(options.DefaultExpirationMinutes),
                
                // Enable Fail-Safe to serve stale data if the database or factory fails
                IsFailSafeEnabled = options.EnableFailSafe,
                FailSafeMaxDuration = TimeSpan.FromMinutes(options.FailSafeMaxDurationMinutes),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(options.FailSafeThrottleDurationSeconds),
                
                // Prevent long-running factory executions from hanging the request
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
            
            // Backplane ensures cache coherence across distributed instances
            fusionBuilder.WithStackExchangeRedisBackplane(redisOpts =>
            {
                redisOpts.Configuration = options.RedisConnection;
            });
        }

        return services;
    }

    /// <summary>
    /// Configures HTTP Response Compression (Gzip/Brotli).
    /// </summary>
    public static IServiceCollection AddServerCompression(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(CompressionOptions.SectionName).Get<CompressionOptions>() 
                      ?? new CompressionOptions();

        if (!options.Enabled) return services;

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = options.EnableForHttps;
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        });

        // Parse compression level enum string to strict Enum
        if (!Enum.TryParse<CompressionLevel>(options.Level, true, out var level))
        {
            level = CompressionLevel.Fastest;
        }

        services.Configure<BrotliCompressionProviderOptions>(opts => opts.Level = level);
        services.Configure<GzipCompressionProviderOptions>(opts => opts.Level = level);

        return services;
    }

    /// <summary>
    /// Configures Rate Limiting with dynamic policies from config and hardcoded secure defaults.
    /// We enforce strict limits on auth endpoints to prevent brute-forcing.
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
                // General: Sliding window for smoothness
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

                // Login: Strict limit to prevent brute force attacks
                if (!registeredNames.Contains(RateLimitPolicies.Login))
                {
                    limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Login, opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });
                }

                // Register: Limit account creation spam
                if (!registeredNames.Contains(RateLimitPolicies.Register))
                {
                    limiterOptions.AddFixedWindowLimiter(RateLimitPolicies.Register, opt =>
                    {
                        opt.PermitLimit = 3;
                        opt.Window = TimeSpan.FromHours(1);
                        opt.QueueLimit = 0;
                    });
                }

                // Email: Prevent spamming verification codes or reset links
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
                
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new MessageResponse("Too many requests. Please try again later." ), token);
            };
        });

        return services;
    }

    /// <summary>
    /// Registers application business services.
    /// </summary>
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        // Core
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<ISiteMetadataService, SiteMetadataService>(); 
        
        // Auth & Identity
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAccountService, AccountService>();
        
        // Profiles & Social
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProfileDetailService, ProfileDetailService>();
        services.AddScoped<ISocialService, SocialService>();
        
        // Infrastructure
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<DbSeedService>();
        
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

    /// <summary>
    /// Configures JWT Authentication and SecurityStamp real-time validation.
    /// </summary>
    public static IServiceCollection AddServerAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var securityOptions = config.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() 
                              ?? throw new InvalidOperationException("Security options are missing.");
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() 
                         ?? new JwtOptions();

        var key = Encoding.ASCII.GetBytes(securityOptions.ApplicationSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata; 
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    
                    var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    var stampClaim = context.Principal?.FindFirst("SecurityStamp");

                    if (userIdClaim == null || stampClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        context.Fail("Invalid token claims.");
                        return;
                    }

                    var currentStamp = await dbContext.AccountCredentials
                        .AsNoTracking()
                        .Where(c => c.AccountId == userId)
                        .Select(c => c.SecurityStamp)
                        .FirstOrDefaultAsync();

                    if (currentStamp == null || currentStamp != stampClaim.Value)
                    {
                        context.Fail("This token has been invalidated due to a security stamp mismatch.");
                    }
                }
            };
        });

        return services;
    }
}
