using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoneypotTrack.Domain.Base;

namespace HoneypotTrack.Domain.Entities;

[Table("categoria", Schema = "empresa")]
public class Categoria : BaseEntity
{
    [Key]
    [Column("categoryid")]
    public int CategoryId { get; set; }

    [Column("name")]
    [StringLength(50)]
    [Required]
    public string Name { get; set; } = null!;

    [Column("operationtype")]
    [StringLength(10)]
    [Required]
    public string OperationType { get; set; } = null!;

    // Propiedades de navegación
    public virtual ICollection<Transaccion> Transacciones { get; set; } = [];
}
