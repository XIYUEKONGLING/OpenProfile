namespace OpenProfileServer.Models.DTOs.Account;

public class DeletionCountdownDto
{
    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public int Seconds { get; set; }
    public bool IsInCooldown { get; set; }
}
