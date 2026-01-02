using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class CertificateDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Content { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Visibility Visibility { get; set; }
}