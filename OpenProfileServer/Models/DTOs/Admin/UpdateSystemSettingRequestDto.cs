using System.ComponentModel.DataAnnotations;

namespace OpenProfileServer.Models.DTOs.Admin;

public class UpdateSystemSettingRequestDto
{
    [Required]
    public string Value { get; set; } = string.Empty;
}