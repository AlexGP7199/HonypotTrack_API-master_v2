namespace HonypotTrack.Application.Dtos.Auth;

/// <summary>
/// DTO para solicitud de refresh token
/// </summary>
public class RefreshTokenRequestDto
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
