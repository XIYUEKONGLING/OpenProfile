namespace OpenProfileServer.Configuration;

public class EmailOptions
{
    public const string SectionName = "Email";

    public bool IsEnabled { get; set; } = false;

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = false;
    public bool UseStartTls { get; set; } = true;

    public string FromAddress { get; set; } = "noreply@example.com";
    public string FromName { get; set; } = "OpenProfile Server";
}