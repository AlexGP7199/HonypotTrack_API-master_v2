using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Usuario;

public class UsuarioFilters : BaseFilters
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
}
