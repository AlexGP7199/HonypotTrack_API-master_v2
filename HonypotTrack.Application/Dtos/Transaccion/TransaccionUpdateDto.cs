using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Transaccion;

public class TransaccionUpdateDto
{
    [Required(ErrorMessage = "El TransaccionId es requerido")]
    public int TransaccionId { get; set; }

    [Required(ErrorMessage = "El AccountId es requerido")]
    public int AccountId { get; set; }

    [Required(ErrorMessage = "El CategoryId es requerido")]
    public int CategoryId { get; set; }

    public int? ContactsId { get; set; }

    [Required(ErrorMessage = "El monto es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Monto { get; set; }

    [StringLength(10)]
    public string Moneda { get; set; } = "USD";

    [StringLength(100)]
    public string? Descripcion { get; set; }

    public DateTime? Fecha { get; set; }
}
