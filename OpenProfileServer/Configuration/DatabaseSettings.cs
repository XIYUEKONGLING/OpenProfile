namespace OpenProfileServer.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";
    
    public string Type { get; set; } = "SQLite";
    public string ConnectionString { get; set; } = "Data Source=openprofile.db";

    public bool IsValid() => 
        !string.IsNullOrWhiteSpace(Type) && 
        !string.IsNullOrWhiteSpace(ConnectionString) &&
        new[] { "SQLite", "PgSQL", "MySQL" }.Contains(Type, StringComparer.OrdinalIgnoreCase);
}