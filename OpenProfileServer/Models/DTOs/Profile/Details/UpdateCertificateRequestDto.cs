using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Profile.Details;

public class UpdateCertificateRequestDto
{
    [Required]
    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string Fingerprint { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(65536)]
    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Visibility? Visibility { get; set; }
}