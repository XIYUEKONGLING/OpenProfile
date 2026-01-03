using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Auth;

public class PublicVerifyEmailRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}