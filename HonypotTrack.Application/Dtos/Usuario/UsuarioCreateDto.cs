using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Usuario;

public class UsuarioCreateDto
{
    [StringLength(100)]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(100)]
    public string Email { get; set; } = null!;
}
