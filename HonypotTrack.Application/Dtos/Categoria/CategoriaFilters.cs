using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Categoria;

public class CategoriaFilters : BaseFilters
{
    public string? Name { get; set; }
    public string? OperationType { get; set; } // Ingreso o Egreso
}
