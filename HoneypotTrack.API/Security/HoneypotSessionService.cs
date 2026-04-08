using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Security;

/// <summary>
/// Servicio para gestionar sesiones honeypot
/// </summary>
public interface IHoneypotSessionService
{
    /// <summary>
    /// Crea una sesión honeypot cuando se detecta un ataque
    /// </summary>
    Task<HoneypotLoginResult> CreateHoneypotSessionAsync(
        string attackerIp, 
        string? userAgent,
        string threatType, 
        string? payload);

    /// <summary>
    /// Verifica si un token es de una sesión honeypot
    /// </summary>
    bool IsHoneypotToken(string token);

    /// <summary>
    /// Obtiene la sesión honeypot activa por token
    /// </summary>
    Task<HoneypotSession?> GetSessionByTokenAsync(string token);

    /// <summary>
    /// Actualiza la actividad de una sesión honeypot
    /// </summary>
    Task UpdateSessionActivityAsync(string token);

    /// <summary>
    /// Incrementa el contador de amenazas de una sesión
    /// </summary>
    Task IncrementThreatCountAsync(string token);
}

/// <summary>
/// Resultado de login honeypot
/// </summary>
public record HoneypotLoginResult
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int AssignedUserId { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? Role { get; init; }
}

public class HoneypotSessionService : IHoneypotSessionService
{
    private readonly HoneypotDbContext _honeypotContext;
    private readonly AppDbContext _mainContext; // Para guardar auditoría
    private readonly IConfiguration _configuration;
    private readonly ILogger<HoneypotSessionService> _logger;

    // Identificador secreto para tokens honeypot (claim oculto)
    private const string HoneypotClaimType = "hpt";
    private const string HoneypotClaimValue = "1";

    public HoneypotSessionService(
        HoneypotDbContext honeypotContext,
        AppDbContext mainContext,
        IConfiguration configuration,
        ILogger<HoneypotSessionService> logger)
    {
        _honeypotContext = honeypotContext;
        _mainContext = mainContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HoneypotLoginResult> CreateHoneypotSessionAsync(
        string attackerIp, 
        string? userAgent,
        string threatType, 
        string? payload)
    {
        try
        {
            var canConnect = await _honeypotContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                const string reason = "No se pudo conectar a la base de datos honeypot.";
                _logger.LogError(reason);
                return new HoneypotLoginResult { Success = false, FailureReason = reason };
            }

            // Seleccionar un usuario falso aleatorio para asignar al atacante
            var fakeUsers = await _honeypotContext.Usuarios.ToListAsync();

            if (!fakeUsers.Any())
            {
                const string reason = "La base honeypot no tiene usuarios falsos cargados.";
                _logger.LogError(reason);
                return new HoneypotLoginResult { Success = false, FailureReason = reason };
            }

            // Asignar usuario aleatorio (el primero es admin)
            var assignedUser = fakeUsers[Random.Shared.Next(fakeUsers.Count)];

            // Generar token JWT con claim honeypot oculto
            var token = GenerateHoneypotToken(assignedUser);
            var refreshToken = GenerateRefreshToken();

            // Crear sesión honeypot
            var session = new HoneypotSession
            {
                SessionToken = token,
                AttackerIp = attackerIp,
                InitialThreatType = threatType,
                InitialPayload = payload?.Length > 2000 ? payload[..2000] : payload,
                UserAgent = userAgent,
                AssignedUserId = assignedUser.UserId,
                IsActive = true,
                StartTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _honeypotContext.HoneypotSessions.Add(session);
            await _honeypotContext.SaveChangesAsync();

            // Registrar en auditoría principal
            await LogHoneypotSessionCreated(session, assignedUser);

            _logger.LogWarning(
                "🍯 SESIÓN HONEYPOT CREADA - IP: {Ip}, Usuario asignado: {User}, Amenaza inicial: {Threat}",
                attackerIp, assignedUser.Email, threatType);

            return new HoneypotLoginResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Token válido por 24h
                AssignedUserId = assignedUser.UserId,
                UserName = assignedUser.FullName ?? "Admin User",
                Email = assignedUser.Email,
                Role = "Admin" // Dar rol de admin para que el atacante tenga "acceso completo"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando sesión honeypot");
            return new HoneypotLoginResult
            {
                Success = false,
                FailureReason = $"Error creando sesión honeypot: {ex.GetType().Name}: {ex.Message}" +
                    $"{(ex.InnerException is not null ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : string.Empty)}"
            };
        }
    }

    public bool IsHoneypotToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Verificar claim honeypot oculto
            var honeypotClaim = jwtToken.Claims
                .FirstOrDefault(c => c.Type == HoneypotClaimType);

            return honeypotClaim?.Value == HoneypotClaimValue;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HoneypotSession?> GetSessionByTokenAsync(string token)
    {
        return await _honeypotContext.HoneypotSessions
            .FirstOrDefaultAsync(s => s.SessionToken == token && s.IsActive);
    }

    public async Task UpdateSessionActivityAsync(string token)
    {
        var session = await GetSessionByTokenAsync(token);
        if (session != null)
        {
            session.LastActivity = DateTime.UtcNow;
            session.TotalRequests++;
            await _honeypotContext.SaveChangesAsync();
        }
    }

    public async Task IncrementThreatCountAsync(string token)
    {
        var session = await GetSessionByTokenAsync(token);
        if (session != null)
        {
            session.TotalThreatsDetected++;
            await _honeypotContext.SaveChangesAsync();
        }
    }

    private string GenerateHoneypotToken(Usuario user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "HoneypotTrack_SuperSecretKey_2024_MinLength32Chars!";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName ?? "Admin User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Claim honeypot oculto - no obvio para el atacante
            new Claim(HoneypotClaimType, HoneypotClaimValue)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "HoneypotTrackAPI",
            audience: jwtSettings["Audience"] ?? "HoneypotTrackClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
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

    private async Task LogHoneypotSessionCreated(HoneypotSession session, Usuario assignedUser)
    {
        var auditLog = new AuditLog
        {
            ActionType = "HONEYPOT_SESSION_CREATED",
            EntityName = "HoneypotSession",
            EntityId = session.SessionId.ToString(),
            IpAddress = session.AttackerIp,
            UserAgent = session.UserAgent,
            HttpMethod = "SYSTEM", // Requerido - no es un HTTP request real
            RequestUrl = "/honeypot/session/create",
            RequestPath = "/honeypot/session/create",
            ErrorMessage = $"Sesión honeypot creada - Usuario asignado: {assignedUser.Email}, Amenaza: {session.InitialThreatType}",
            ExceptionDetails = $"Payload inicial: {session.InitialPayload}",
            Timestamp = DateTime.UtcNow,
            LocalTimestamp = DateTime.Now,
            IsSuccessful = true,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        _mainContext.AuditLogs.Add(auditLog);
        await _mainContext.SaveChangesAsync();
    }
}
