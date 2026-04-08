namespace HonypotTrack.Application.Dtos.Usuario;

public class UsuarioDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = null!;
}
