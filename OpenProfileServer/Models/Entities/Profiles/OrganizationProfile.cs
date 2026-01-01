using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenProfileServer.Models.Entities.Base;

namespace OpenProfileServer.Models.Entities.Profiles;

[Table("OrganizationProfiles")]
public class OrganizationProfile : Profile
{
    public DateOnly? FoundedDate { get; set; }

    [MaxLength(64)]
    public string? TaxId { get; set; }

    // Navigation: Members of this organization
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}