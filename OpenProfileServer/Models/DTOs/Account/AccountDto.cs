using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Account;

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Primary Email
    public AccountType Type { get; set; }
    public AccountRole Role { get; set; }
    public AccountStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}