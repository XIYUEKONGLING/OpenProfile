using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Interfaces;

namespace OpenProfileServer.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ISystemSettingService _settingService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailOptions> emailOptions,
        ISystemSettingService settingService,
        ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _settingService = settingService;
        _logger = logger;
    }

    public bool IsEnabled => _emailOptions.IsEnabled && !string.IsNullOrWhiteSpace(_emailOptions.Host);

    public async Task<bool> CheckConnectionAsync()
    {
        if (!IsEnabled) return false;

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_emailOptions.Host, _emailOptions.Port, 
                _emailOptions.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(_emailOptions.Username))
            {
                await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password);
            }
            
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP Connection Check Failed.");
            return false;
        }
    }

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string username, string code)
    {
        var subjectTemplate = await _settingService.GetValueAsync(SystemSettingKeys.EmailVerificationSubject) 
                              ?? "Verify your email - OpenProfile";
        
        var bodyTemplate = await _settingService.GetValueAsync(SystemSettingKeys.EmailVerificationBody) 
                           ?? "Hello {Username}, your verification code is: {Code}";

        var subject = subjectTemplate
            .Replace("{Username}", username)
            .Replace("{Code}", code);

        var body = bodyTemplate
            .Replace("{Username}", username)
            .Replace("{Code}", code);

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("EmailService is disabled. Message to {Email} was skipped.", toEmail);
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromAddress));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Note: Validation callback can be customized for self-signed certs in Dev environments if needed.
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(_emailOptions.Host, _emailOptions.Port, 
                _emailOptions.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(_emailOptions.Username))
            {
                await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email successfully sent to {Email}.", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}.", toEmail);
            return false;
        }
    }
}
