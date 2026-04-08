using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Cuenta;

public class CuentaUpdateDto
{
    [Required(ErrorMessage = "El AccountId es requerido")]
    public int AccountId { get; set; }

    [Required(ErrorMessage = "El UserId es requerido")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "El nombre de la cuenta es requerido")]
    [StringLength(50)]
    public string AccountName { get; set; } = null!;

    [StringLength(3)]
    public string Currency { get; set; } = "USD";
}
