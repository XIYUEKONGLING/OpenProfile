using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Data;
using OpenProfileServer.Extensions;

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
        // 1. Controller & JSON Configuration (PascalCase)
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
            });

        // 2. Core Infrastructure (via Extensions)
        builder.Services.AddDatabaseContext(builder.Configuration);
        builder.Services.AddServerCaching(builder.Configuration);      // FusionCache + Redis (Dynamic Options)
        builder.Services.AddServerRateLimiting(builder.Configuration); // Rate Limiting (Dynamic Policies)

        // 3. Health Checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        // 4. OpenAPI / Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
    }

    private static async Task InitializeAppAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Trigger validation for critical settings immediately upon startup
            var dbSettings = services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            _ = services.GetRequiredService<IOptions<CacheOptions>>().Value;
            _ = services.GetRequiredService<IOptions<SecurityOptions>>().Value;
            _ = services.GetRequiredService<IOptions<RateLimitOptions>>().Value;

            logger.LogInformation("Starting application in {Environment} mode", app.Environment.EnvironmentName);
            logger.LogInformation("Using database provider: {Provider}", dbSettings.Type);
            
            // Database Migration
            var context = services.GetRequiredService<ApplicationDbContext>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying pending migrations...");
                await context.Database.MigrateAsync();
            }

            logger.LogInformation("Database initialization completed successfully.");
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
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/api.json");

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/api.json", "OpenProfileServer API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        
        app.UseRateLimiter();
        
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
    }
}
