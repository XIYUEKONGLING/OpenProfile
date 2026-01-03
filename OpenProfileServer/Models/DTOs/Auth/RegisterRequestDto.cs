using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [MaxLength(64)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Required if 'RegistrationRequiresEmail' is enabled.
    /// </summary>
    public string? Code { get; set; }
}