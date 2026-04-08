using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Auth;
using HonypotTrack.Application.Interfaces;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HonypotTrack.Application.Services;

/// <summary>
/// Servicio de autenticación
/// </summary>
public class AuthService(IUnitOfWork unitOfWork, IConfiguration configuration) : IAuthService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IConfiguration _configuration = configuration;

    public async Task<BaseResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        // Buscar usuario por email
        var usuario = await _unitOfWork.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (usuario == null)
        {
            return BaseResponse<LoginResponseDto>.Fail("Credenciales inválidas");
        }

        // Verificar contraseña
        if (string.IsNullOrEmpty(usuario.PasswordHash) || !VerifyPassword(request.Password, usuario.PasswordHash))
        {
            return BaseResponse<LoginResponseDto>.Fail("Credenciales inválidas");
        }

        // Generar tokens
        var token = GenerateJwtToken(usuario);
        var refreshToken = GenerateRefreshToken();

        // Guardar refresh token
        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            UserId = usuario.UserId,
            FullName = usuario.FullName ?? string.Empty,
            Email = usuario.Email,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshToken
        }, "Login exitoso");
    }

    public async Task<BaseResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        // Verificar si el email ya existe
        var existingUser = await _unitOfWork.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return BaseResponse<LoginResponseDto>.Fail("El email ya está registrado");
        }

        // Crear nuevo usuario
        var usuario = new Usuario
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Usuarios.AddAsync(usuario);
        await _unitOfWork.SaveChangesAsync();

        // Generar tokens
        var token = GenerateJwtToken(usuario);
        var refreshToken = GenerateRefreshToken();

        // Guardar refresh token
        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            UserId = usuario.UserId,
            FullName = usuario.FullName ?? string.Empty,
            Email = usuario.Email,
            Token = token,
            TokenExpiration = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshToken
        }, "Registro exitoso");
    }

    public async Task<BaseResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        // Validar el token JWT expirado
        var principal = GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return BaseResponse<LoginResponseDto>.Fail("Token inválido");
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return BaseResponse<LoginResponseDto>.Fail("Token inválido");
        }

        // Buscar usuario
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
        if (usuario == null || usuario.RefreshToken != request.RefreshToken || 
            usuario.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            return BaseResponse<LoginResponseDto>.Fail("Refresh token inválido o expirado");
        }

        // Generar nuevos tokens
        var newToken = GenerateJwtToken(usuario);
        var newRefreshToken = GenerateRefreshToken();

        // Actualizar refresh token
        usuario.RefreshToken = newRefreshToken;
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            UserId = usuario.UserId,
            FullName = usuario.FullName ?? string.Empty,
            Email = usuario.Email,
            Token = newToken,
            TokenExpiration = DateTime.UtcNow.AddHours(1),
            RefreshToken = newRefreshToken
        }, "Token renovado exitosamente");
    }

    public async Task<BaseResponse<bool>> LogoutAsync(int userId)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
        if (usuario == null)
        {
            return BaseResponse<bool>.Fail("Usuario no encontrado");
        }

        // Invalidar refresh token
        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiry = null;
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<bool>.Success(true, "Sesión cerrada exitosamente");
    }

    public async Task<BaseResponse<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
        if (usuario == null)
        {
            return BaseResponse<bool>.Fail("Usuario no encontrado");
        }

        // Verificar contraseña actual
        if (string.IsNullOrEmpty(usuario.PasswordHash) || !VerifyPassword(currentPassword, usuario.PasswordHash))
        {
            return BaseResponse<bool>.Fail("Contraseña actual incorrecta");
        }

        // Actualizar contraseña
        usuario.PasswordHash = HashPassword(newPassword);
        usuario.FechaActualizacion = DateTime.UtcNow;
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<bool>.Success(true, "Contraseña actualizada exitosamente");
    }

    #region Private Methods

    private string GenerateJwtToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "HoneypotTrack_SuperSecretKey_2024_MinLength32Chars!";
        var issuer = jwtSettings["Issuer"] ?? "HoneypotTrackAPI";
        var audience = jwtSettings["Audience"] ?? "HoneypotTrackClient";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.UserId.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.FullName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "HoneypotTrack_SuperSecretKey_2024_MinLength32Chars!";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false // Permitir tokens expirados
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == hash;
    }

    #endregion
}
