namespace OpenProfileServer.Models.DTOs.Auth;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; } = null;
    public DateTime ExpiresAt { get; set; }
}