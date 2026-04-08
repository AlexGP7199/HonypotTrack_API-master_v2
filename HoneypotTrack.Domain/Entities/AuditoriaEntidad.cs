using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoneypotTrack.Domain.Entities;

[Table("AuditoriaEntidades", Schema = "empresa")]
public class AuditoriaEntidad
{
    [Key]
    [Column("AuditoriaId")]
    public int AuditoriaId { get; set; }

    [Column("NombreTabla")]
    [StringLength(100)]
    [Required]
    public string NombreTabla { get; set; } = null!;

    [Column("LlavePrimaria")]
    [StringLength(50)]
    [Required]
    public string LlavePrimaria { get; set; } = null!;

    [Column("Accion")]
    [StringLength(20)]
    [Required]
    public string Accion { get; set; } = null!; // Added, Modified, Deleted

    [Column("ValorAnterior")]
    public string? ValorAnterior { get; set; }

    [Column("ValorNuevo")]
    public string? ValorNuevo { get; set; }

    [Column("ColumnasCambiadas")]
    [StringLength(500)]
    public string? ColumnasCambiadas { get; set; }

    // Información del usuario
    [Column("UsuarioId")]
    public int? UsuarioId { get; set; }

    [Column("UsuarioNombre")]
    [StringLength(100)]
    public string? UsuarioNombre { get; set; }

    [Column("UsuarioEmail")]
    [StringLength(100)]
    public string? UsuarioEmail { get; set; }

    // Información de la petición
    [Column("IpAddress")]
    [StringLength(50)]
    public string? IpAddress { get; set; }

    [Column("Path")]
    [StringLength(300)]
    public string? Path { get; set; }

    [Column("MetodoHttp")]
    [StringLength(10)]
    public string? MetodoHttp { get; set; }

    [Column("UserAgent")]
    [StringLength(500)]
    public string? UserAgent { get; set; }

    [Column("CorrelationId")]
    [StringLength(50)]
    public string? CorrelationId { get; set; }

    // Timestamps
    [Column("FechaCambio")]
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    [Column("FechaCambioLocal")]
    public DateTime FechaCambioLocal { get; set; } = DateTime.Now;
}
