using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Contact;

public class ContactUpdateDto
{
    [Required(ErrorMessage = "El ContactsId es requerido")]
    public int ContactsId { get; set; }

    [Required(ErrorMessage = "El UserId es requerido")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "El tipo es requerido")]
    [StringLength(50)]
    public string Type { get; set; } = null!;

    [Required(ErrorMessage = "El TaxId es requerido")]
    [StringLength(20)]
    public string TaxId { get; set; } = null!;
}
