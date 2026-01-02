using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Data;
using OpenProfileServer.Extensions;
using OpenProfileServer.Services;

namespace OpenProfileServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        RegisterConfiguration(builder);
        RegisterServices(builder);

        var app = builder.Build();

        await InitializeAppAsync(app);

        ConfigureMiddleware(app);

        await app.RunAsync();
    }

    private static void RegisterConfiguration(WebApplicationBuilder builder)
    {
        builder.Services.AddServerConfiguration(builder.Configuration);
    }

    private static void RegisterServices(WebApplicationBuilder builder)
    {
        // 1. Configure Minimal API JSON Options (Used by AddOpenApi for schema generation)
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null; // PascalCase
            options.SerializerOptions.PropertyNameCaseInsensitive = true; // Allow camelCase input
        });
        
        // 2. Controller & JSON Configuration (PascalCase)
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase to match .NET standards
                // options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });

        // 3. Core Infrastructure (via Extensions)
        builder.Services.AddDatabaseContext(builder.Configuration);
        builder.Services.AddServerCaching(builder.Configuration);      // FusionCache + Redis (Dynamic Options)
        builder.Services.AddServerRateLimiting(builder.Configuration); // Rate Limiting (Dynamic Policies)
        builder.Services.AddServerCompression(builder.Configuration);  // Response Compression (Gzip/Brotli)
        
        // 4. Application Services
        builder.Services.AddServerServices();

        // 5. Health Checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        // 6. OpenAPI / Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
    }

    private static async Task InitializeAppAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        try
        {
            // Trigger validation for critical settings immediately upon startup.
            // If configuration is invalid (e.g., missing secrets), the app will crash here intentionally.
            _ = services.GetRequiredService<IOptions<ApplicationOptions>>().Value;
            var dbSettings = services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            _ = services.GetRequiredService<IOptions<CacheOptions>>().Value;
            _ = services.GetRequiredService<IOptions<SecurityOptions>>().Value;
            _ = services.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            _ = services.GetRequiredService<IOptions<JwtOptions>>().Value;
            _ = services.GetRequiredService<IOptions<EmailOptions>>().Value;
            _ = services.GetRequiredService<IOptions<CompressionOptions>>().Value;
            _ = services.GetRequiredService<IOptions<StorageOptions>>().Value;

            logger.LogInformation("Starting application in {Environment} mode", app.Environment.EnvironmentName);
            logger.LogInformation("Using database provider: {Provider}", dbSettings.Type);
            
            // Database Migration Logic
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (env.IsDevelopment())
            {
                // For SQLite in Dev, this creates the .db file and all tables 
                logger.LogInformation("Ensuring database is created (Development)...");
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                // In production, we apply migrations safely
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                }
            }

            // Data Seeding (Root Account and Default System Settings)
            var seeder = services.GetRequiredService<DbSeedService>();
            await seeder.SeedAsync();

            logger.LogInformation("Database initialization and seeding completed successfully.");
        }
        catch (OptionsValidationException ex)
        {
            logger.LogCritical("Configuration Validation Failed: {Errors}", string.Join(", ", ex.Failures));
            throw;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred during application startup sequence.");
            throw;
        }
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        var compressionOptions = app.Services.GetRequiredService<IOptions<CompressionOptions>>().Value;
        
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/api.json");

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/api.json", "OpenProfileServer API");
                options.RoutePrefix = "swagger";
            });
        }
        
        // Response Compression should run early to compress static files or API responses
        if (compressionOptions.Enabled)
        {
            app.UseResponseCompression();
        }

        app.UseHttpsRedirection();
        
        // Rate Limiting needs to run before expensive operations but after HTTPS
        app.UseRateLimiter();
        
        app.UseAuthentication(); 
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
    }
}
