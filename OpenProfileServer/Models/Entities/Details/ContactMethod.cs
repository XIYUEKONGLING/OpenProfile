using System.ComponentModel.DataAnnotations;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Models.Entities.Details;

public class ContactMethod
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    public ContactType Type { get; set; } = ContactType.Email;

    [MaxLength(128)]
    public string Label { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;
    
    public Asset Icon { get; set; } = new();
    public Asset Image { get; set; } = new();

    public Visibility Visibility { get; set; } = Visibility.Private;
}