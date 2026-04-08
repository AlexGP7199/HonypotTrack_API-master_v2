using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Auth;

namespace HonypotTrack.Application.Interfaces;

/// <summary>
/// Interfaz para el servicio de autenticación
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica un usuario con email y contraseña
    /// </summary>
    Task<BaseResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    Task<BaseResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Renueva el token de acceso usando el refresh token
    /// </summary>
    Task<BaseResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Cierra la sesión del usuario (invalida el refresh token)
    /// </summary>
    Task<BaseResponse<bool>> LogoutAsync(int userId);

    /// <summary>
    /// Cambia la contraseña del usuario
    /// </summary>
    Task<BaseResponse<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
