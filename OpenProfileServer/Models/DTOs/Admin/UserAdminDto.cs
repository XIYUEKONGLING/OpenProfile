using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class UserAdminDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountRole Role { get; set; }
    public AccountStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
}