namespace OpenProfileServer.Models.DTOs.Admin;

public class AdminUpdateEmailRequestDto
{
    public bool? IsVerified { get; set; }
    public bool? IsPrimary { get; set; }
}