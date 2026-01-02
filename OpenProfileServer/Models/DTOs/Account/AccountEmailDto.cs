namespace OpenProfileServer.Models.DTOs.Account;

public class AccountEmailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}