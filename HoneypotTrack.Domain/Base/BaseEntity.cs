using System.ComponentModel.DataAnnotations.Schema;

namespace HoneypotTrack.Domain.Base;

public abstract class BaseEntity
{
    [Column("FechaCreacion")]
    public DateTime? FechaCreacion { get; set; }

    [Column("FechaActualizacion")]
    public DateTime? FechaActualizacion { get; set; }
}
