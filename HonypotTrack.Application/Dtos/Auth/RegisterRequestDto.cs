using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Auth;

/// <summary>
/// DTO para registro de nuevo usuario
/// </summary>
public class RegisterRequestDto
{
    [StringLength(100)]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = null!;
}
