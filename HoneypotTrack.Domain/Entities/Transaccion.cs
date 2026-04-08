using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoneypotTrack.Domain.Base;

namespace HoneypotTrack.Domain.Entities;

[Table("transacciones", Schema = "empresa")]
public class Transaccion : BaseEntity
{
    [Key]
    [Column("transaccionid")]
    public int TransaccionId { get; set; }

    [Column("accountid")]
    [Required]
    public int AccountId { get; set; }

    [Column("categoryid")]
    [Required]
    public int CategoryId { get; set; }

    [Column("contactsid")]
    public int? ContactsId { get; set; }

    [Column("Monto")]
    public decimal Monto { get; set; }

    [Column("moneda")]
    [StringLength(10)]
    public string Moneda { get; set; } = "USD";

    [Column("Descripcion")]
    [StringLength(100)]
    public string? Descripcion { get; set; }

    [Column("fecha")]
    public DateTime? Fecha { get; set; }

    // Propiedades de navegación
    [ForeignKey(nameof(AccountId))]
    public virtual Cuenta Cuenta { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Categoria Categoria { get; set; } = null!;

    [ForeignKey(nameof(ContactsId))]
    public virtual Contact? Contact { get; set; }
}
