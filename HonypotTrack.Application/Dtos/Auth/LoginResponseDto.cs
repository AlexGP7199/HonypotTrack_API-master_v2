namespace HonypotTrack.Application.Dtos.Auth;

/// <summary>
/// DTO para respuesta de login exitoso
/// </summary>
public class LoginResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime TokenExpiration { get; set; }
    public string RefreshToken { get; set; } = null!;
}
