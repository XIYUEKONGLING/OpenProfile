namespace OpenProfileServer.Models.DTOs.Auth;

public class AuthConfigDto
{
    /// <summary>
    /// If true, the client must call /send-code first, and include the code in /register.
    /// </summary>
    public bool RegistrationRequiresEmail { get; set; }
    
    public bool AllowRegistration { get; set; }
}