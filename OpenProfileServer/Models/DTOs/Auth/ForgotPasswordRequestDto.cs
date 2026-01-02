using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}