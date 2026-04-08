using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Categoria;

public class CategoriaCreateDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "El tipo de operación es requerido")]
    [StringLength(10)]
    public string OperationType { get; set; } = null!; // Ingreso o Egreso
}
