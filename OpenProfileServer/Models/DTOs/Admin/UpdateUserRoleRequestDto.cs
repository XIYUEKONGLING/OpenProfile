using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class UpdateUserRoleRequestDto
{
    public AccountRole Role { get; set; }
}