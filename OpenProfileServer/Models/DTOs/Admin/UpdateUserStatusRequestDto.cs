using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Admin;

public class UpdateUserStatusRequestDto
{
    public AccountStatus Status { get; set; }
}