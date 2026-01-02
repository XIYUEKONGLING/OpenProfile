namespace OpenProfileServer.Models.DTOs.Social;

public class BlockDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
}
