using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Configuration;
using OpenProfileServer.Data;

namespace OpenProfileServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() 
                       ?? new DatabaseSettings();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            _ = settings.Type.ToUpperInvariant() switch
            {
                "SQLITE" => options.UseSqlite(settings.ConnectionString),
                "PGSQL"  => options.UseNpgsql(settings.ConnectionString),
                "MYSQL"  => options.UseMySql(settings.ConnectionString, ServerVersion.AutoDetect(settings.ConnectionString)),
                _ => throw new InvalidOperationException($"Unsupported database type: {settings.Type}")
            };
        });

        return services;
    }
}