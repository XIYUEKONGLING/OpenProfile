using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Models.DTOs.Organization;

public class InviteMemberRequestDto
{
    [Required]
    public string Identity { get; set; } = string.Empty;

    public MemberRole Role { get; set; } = MemberRole.Member;
    
    public string? Title { get; set; }
}