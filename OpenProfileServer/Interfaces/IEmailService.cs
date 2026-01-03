namespace OpenProfileServer.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Checks if the email service is enabled in configuration and ready to use.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Attempts to connect to the SMTP server to verify credentials.
    /// Useful for diagnostics or admin settings check.
    /// </summary>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Sends a verification email. Returns true if successful.
    /// </summary>
    Task<bool> SendVerificationEmailAsync(string toEmail, string username, string code);

    /// <summary>
    /// Sends a generic email. Returns true if successful.
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}