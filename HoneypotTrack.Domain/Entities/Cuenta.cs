using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoneypotTrack.Domain.Base;

namespace HoneypotTrack.Domain.Entities;

[Table("Cuenta", Schema = "empresa")]
public class Cuenta : BaseEntity
{
    [Key]
    [Column("accountid")]
    public int AccountId { get; set; }

    [Column("Userid")]
    [Required]
    public int UserId { get; set; }

    [Column("account_name")]
    [StringLength(50)]
    [Required]
    public string AccountName { get; set; } = null!;

    [Column("Currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    // Propiedades de navegación
    [ForeignKey(nameof(UserId))]
    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<Transaccion> Transacciones { get; set; } = [];
}
