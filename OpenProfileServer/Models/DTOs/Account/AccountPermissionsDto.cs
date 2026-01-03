using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Account;

public class AccountPermissionsDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountRole Role { get; set; }
}