using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class UserFilterDto
{
    public string? Search { get; set; }
    public AccountStatus? Status { get; set; }
    public AccountRole? Role { get; set; }
}