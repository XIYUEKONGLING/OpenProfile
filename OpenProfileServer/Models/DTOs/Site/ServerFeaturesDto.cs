namespace OpenProfileServer.Models.DTOs.Site;

public class ServerFeaturesDto
{
    /// <summary>
    /// Indicates if the SMTP Email service is configured and enabled.
    /// If false, features like "Forgot Password" or "Email Verification" should be hidden.
    /// </summary>
    public bool Email { get; set; }

    /// <summary>
    /// Indicates if new user registration is currently allowed.
    /// </summary>
    public bool Registration { get; set; }

    /// <summary>
    /// Indicates if the site allows search engine indexing (robots.txt logic).
    /// </summary>
    public bool SearchIndexing { get; set; }
    
    /// <summary>
    /// Indicates if the server requires email verification before login.
    /// </summary>
    public bool EmailVerification { get; set; }
}