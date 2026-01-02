using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Auth;

public class LoginRequestDto
{
    /// <summary>
    /// Can be Email or AccountName.
    /// </summary>
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}