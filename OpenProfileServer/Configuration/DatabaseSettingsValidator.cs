using Microsoft.Extensions.Options;

namespace OpenProfileServer.Configuration;

public class DatabaseSettingsValidator : IValidateOptions<DatabaseSettings>
{
    public ValidateOptionsResult Validate(string? name, DatabaseSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Type))
            return ValidateOptionsResult.Fail("DatabaseSettings.Type is required.");
        
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            return ValidateOptionsResult.Fail("DatabaseSettings.ConnectionString is required.");
        
        var validTypes = new[] { "SQLite", "PgSQL", "MySQL" };
        if (!validTypes.Contains(settings.Type, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"Invalid Database Type: {settings.Type}. Support: {string.Join(", ", validTypes)}");
        }
        
        return ValidateOptionsResult.Success;
    }
}