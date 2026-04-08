using System.ComponentModel.DataAnnotations;

namespace HonypotTrack.Application.Dtos.Auth;

/// <summary>
/// DTO para cambio de contraseña
/// </summary>
public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmNewPassword { get; set; } = null!;
}
