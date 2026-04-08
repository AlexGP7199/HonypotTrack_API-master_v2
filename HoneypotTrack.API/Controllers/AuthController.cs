using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HonypotTrack.Application.Dtos.Auth;
using HonypotTrack.Application.Interfaces;
using HonypotTrack.Application.Commons.Bases;
using System.Security.Claims;
using System.Text.Json;
using HoneypotTrack.API.Security;
using Microsoft.Extensions.Hosting;

namespace HoneypotTrack.API.Controllers;

/// <summary>
/// Controlador de autenticaci�n
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILoginAttemptTracker loginAttemptTracker,
    IHoneypotSessionService honeypotSessionService,
    IConfiguration configuration) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILoginAttemptTracker _loginAttemptTracker = loginAttemptTracker;
    private readonly IHoneypotSessionService _honeypotSessionService = honeypotSessionService;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Inicia sesi�n con email y contrase�a
    /// </summary>
    /// <param name="request">Credenciales del usuario</param>
    /// <returns>Token de acceso y refresh token</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        var clientIp = GetClientIp();

        if (response.IsSuccess)
        {
            _loginAttemptTracker.Reset(clientIp, request.Email);
            return Ok(response);
        }

        var decision = _loginAttemptTracker.RegisterFailure(clientIp, request.Email);
        if (ShouldActivateBehavioralHoneypot(decision))
        {
            var honeypotResult = await _honeypotSessionService.CreateHoneypotSessionAsync(
                clientIp ?? "unknown",
                Request.Headers.UserAgent.ToString(),
                "FAILED_LOGIN_THRESHOLD",
                JsonSerializer.Serialize(request));

            if (honeypotResult.Success)
            {
                return Ok(new
                {
                    isSuccess = true,
                    message = "Login exitoso",
                    data = new
                    {
                        token = honeypotResult.Token,
                        refreshToken = honeypotResult.RefreshToken,
                        expiresAt = honeypotResult.ExpiresAt,
                        user = new
                        {
                            id = honeypotResult.AssignedUserId,
                            email = honeypotResult.Email,
                            name = honeypotResult.UserName,
                            role = honeypotResult.Role
                        }
                    }
                });
            }

            if (HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    isSuccess = false,
                    message = "Falló la activación del honeypot en login.",
                    detail = honeypotResult.FailureReason ?? "Razón no disponible"
                });
            }
        }

        // Retornar 401 con el body de respuesta
        return StatusCode(StatusCodes.Status401Unauthorized, response);
    }

    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    /// <param name="request">Datos del nuevo usuario</param>
    /// <returns>Token de acceso y refresh token</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var response = await _authService.RegisterAsync(request);

        return response.IsSuccess
            ? CreatedAtAction(nameof(Login), response)
            : BadRequest(response);
    }

    /// <summary>
    /// Renueva el token de acceso usando el refresh token
    /// </summary>
    /// <param name="request">Token actual y refresh token</param>
    /// <returns>Nuevo token de acceso y refresh token</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request);

        return response.IsSuccess
            ? Ok(response)
            : Unauthorized(response);
    }

    /// <summary>
    /// Cierra la sesi�n del usuario actual
    /// </summary>
    /// <returns>Confirmaci�n de cierre de sesi�n</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _authService.LogoutAsync(userId);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    /// <summary>
    /// Cambia la contrase�a del usuario actual
    /// </summary>
    /// <param name="request">Contrase�a actual y nueva contrase�a</param>
    /// <returns>Confirmaci�n del cambio</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    /// <summary>
    /// Obtiene la informaci�n del usuario autenticado
    /// </summary>
    /// <returns>Informaci�n del usuario</returns>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            FullName = name
        });
    }

    private bool ShouldActivateBehavioralHoneypot(FailedLoginDecision decision)
    {
        var enabled = _configuration.GetValue<bool?>("Security:Honeypot:EnableBehavioralDetection") ?? true;
        return enabled && decision.ShouldActivateHoneypot;
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
