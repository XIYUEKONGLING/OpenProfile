using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Account;

public class ChangePasswordRequestDto
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}