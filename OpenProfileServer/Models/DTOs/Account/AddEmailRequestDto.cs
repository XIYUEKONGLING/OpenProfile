using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Account;

public class AddEmailRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}