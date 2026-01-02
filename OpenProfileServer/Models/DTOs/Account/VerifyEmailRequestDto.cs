using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Account;

public class VerifyEmailRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
}