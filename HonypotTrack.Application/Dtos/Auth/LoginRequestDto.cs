using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Auth;

/// <summary>
/// DTO para solicitud de login
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "El email es requerido")]
    // [EmailAddress(ErrorMessage = "Email inválido")] // Deshabilitado para permitir pruebas de seguridad en login
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contrase�a es requerida")]
    public string Password { get; set; } = null!;
}
