using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class CreateUserRequestDto
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

    public AccountType Type { get; set; } = AccountType.Personal;
    
    // Admins can create normal users or other admins (but likely not Root)
    public AccountRole Role { get; set; } = AccountRole.User; 
    
    // Optional display name for the profile
    public string? DisplayName { get; set; }
}