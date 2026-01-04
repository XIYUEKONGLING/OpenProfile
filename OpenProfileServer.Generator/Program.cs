using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenProfileServer.Extensions;

namespace OpenProfileServer.Generator;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Manual Argument Parsing
        var outputDir = "./static-export";
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputDir = args[i + 1];
                i++; // Skip next
            }
        }

        // 2. Build Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 3. Configure Services
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting OpenProfileServer Generator...");
        logger.LogInformation("Target Output Directory: {OutputDir}", Path.GetFullPath(outputDir));

        // 4. Run Generator
        try
        {
            // Create a scope because most services (DbContext, Managers) are registered as Scoped
            using var scope = serviceProvider.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<StaticGenerator>();
            
            await generator.GenerateAsync(outputDir);
            
            logger.LogInformation("Static site generation completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An error occurred during generation.");
            Environment.Exit(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Reuse the exact same setup logic as the API
        services.AddServerConfiguration(configuration);
        services.AddDatabaseContext(configuration);
        
        // We add caching (Memory) even for the generator to satisfy service dependencies
        services.AddServerCaching(configuration);
        
        // Core Business Logic
        services.AddServerServices();

        // Generator Logic
        services.AddScoped<StaticGenerator>();
    }
}
