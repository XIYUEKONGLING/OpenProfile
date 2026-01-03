using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Auth;

public class SendCodeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Context: Registration, VerifyEmail (for adding secondary), ResetPassword, etc.
    /// </summary>
    public VerificationType Type { get; set; }
}