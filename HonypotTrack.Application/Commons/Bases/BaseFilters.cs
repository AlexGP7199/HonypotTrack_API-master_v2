namespace HonypotTrack.Application.Commons.Bases;

public class BaseFilters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Ordenamiento
    public string? OrderBy { get; set; }
    public bool IsDescending { get; set; } = false;

    // Búsqueda general
    public string? Search { get; set; }

    // Filtro por fechas
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
