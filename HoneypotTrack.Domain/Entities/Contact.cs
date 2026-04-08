using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoneypotTrack.Domain.Base;

namespace HoneypotTrack.Domain.Entities;

[Table("Contacts", Schema = "empresa")]
public class Contact : BaseEntity
{
    [Key]
    [Column("contactsid")]
    public int ContactsId { get; set; }

    [Column("userid")]
    [Required]
    public int UserId { get; set; }

    [Column("name")]
    [StringLength(100)]
    [Required]
    public string Name { get; set; } = null!;

    [Column("type")]
    [StringLength(50)]
    [Required]
    public string Type { get; set; } = null!;

    [Column("Taxid")]
    [StringLength(20)]
    [Required]
    public string TaxId { get; set; } = null!;

    // Propiedades de navegación
    [ForeignKey(nameof(UserId))]
    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<Transaccion> Transacciones { get; set; } = [];
}
