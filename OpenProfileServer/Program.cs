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
        builder.Services.Configure<DatabaseSettings>(
            builder.Configuration.GetSection(DatabaseSettings.SectionName));

        builder.Services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
    }

    private static void RegisterServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
                // options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        // OpenAPI / Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        builder.Services.AddDatabaseContext(builder.Configuration);
    }

    private static async Task InitializeAppAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var dbSettings = services.GetRequiredService<IOptions<DatabaseSettings>>().Value;

            logger.LogInformation("Starting application in {Environment} mode", app.Environment.EnvironmentName);
            logger.LogInformation("Using database provider: {Provider}", dbSettings.Type);

            var context = services.GetRequiredService<ApplicationDbContext>();

            // Check if we can connect and apply migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying pending migrations...");
                await context.Database.MigrateAsync();
            }

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred during application startup sequence");
            throw;
        }
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // OpenAPI document at /openapi/api.json
            app.MapOpenApi("/openapi/api.json");

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/api.json", "OpenProfileServer API");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Standard health check endpoint
        app.MapHealthChecks("/health");

        app.MapControllers();
    }
}
