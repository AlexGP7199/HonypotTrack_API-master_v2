using Microsoft.AspNetCore.Mvc;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Controllers.Honeypot;

/// <summary>
/// ?? HONEYPOT CONTROLLER - Endpoints señuelo para atraer atacantes
/// Estos endpoints parecen vulnerables pero solo registran la actividad maliciosa
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] // Ocultar de Swagger (los atacantes los descubren por escaneo)
public class AdminController(AppDbContext dbContext, ILogger<AdminController> logger) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<AdminController> _logger = logger;

    /// <summary>
    /// ?? Señuelo: Login de administrador
    /// Los atacantes intentarán acceder aquí con credenciales por defecto
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> FakeAdminLogin([FromBody] FakeLoginRequest request)
    {
        await LogHoneypotActivity("ADMIN_LOGIN_ATTEMPT", new
        {
            Username = request.Username,
            Password = "***CAPTURED***",
            ActualPassword = request.Password // Capturamos la contraseña que intentaron
        });

        // Simular delay de autenticación
        await Task.Delay(Random.Shared.Next(500, 1500));

        // Siempre fallar pero dar esperanza al atacante
        return Unauthorized(new
        {
            error = "Invalid credentials",
            message = "Authentication failed. Please check your username and password.",
            attempts_remaining = Random.Shared.Next(1, 3)
        });
    }

    /// <summary>
    /// ?? Señuelo: Panel de configuración
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> FakeConfig()
    {
        await LogHoneypotActivity("CONFIG_ACCESS_ATTEMPT", null);

        return Ok(new
        {
            database_host = "db.internal.honeypot.local",
            database_name = "production_db",
            api_key = "sk_live_FAKE_KEY_FOR_TRACKING_" + Guid.NewGuid().ToString("N")[..8],
            aws_access_key = "AKIAFAKE" + Guid.NewGuid().ToString("N")[..16].ToUpper(),
            debug_mode = true,
            version = "2.1.3"
        });
    }

    /// <summary>
    /// ?? Señuelo: Backup de base de datos
    /// </summary>
    [HttpGet("backup")]
    public async Task<IActionResult> FakeBackup()
    {
        await LogHoneypotActivity("BACKUP_ACCESS_ATTEMPT", null);

        return Ok(new
        {
            message = "Backup initiated",
            backup_url = "/downloads/backup_" + DateTime.Now.ToString("yyyyMMdd") + ".sql",
            size = "2.3 GB",
            status = "pending"
        });
    }

    /// <summary>
    /// ?? Señuelo: Lista de usuarios (con datos falsos)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> FakeUsersList()
    {
        await LogHoneypotActivity("USERS_LIST_ATTEMPT", null);

        var fakeUsers = new[]
        {
            new { id = 1, username = "admin", email = "admin@company.com", role = "superadmin", api_key = "key_" + Guid.NewGuid().ToString("N")[..8] },
            new { id = 2, username = "developer", email = "dev@company.com", role = "developer", api_key = "key_" + Guid.NewGuid().ToString("N")[..8] },
            new { id = 3, username = "support", email = "support@company.com", role = "support", api_key = "key_" + Guid.NewGuid().ToString("N")[..8] }
        };

        return Ok(new { users = fakeUsers, total = fakeUsers.Length });
    }

    /// <summary>
    /// ?? Señuelo: Ejecutar comando (Command Injection trap)
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> FakeExecute([FromBody] FakeExecuteRequest request)
    {
        await LogHoneypotActivity("COMMAND_EXECUTION_ATTEMPT", new
        {
            Command = request.Command,
            Args = request.Args
        });

        // Simular ejecución
        await Task.Delay(Random.Shared.Next(1000, 3000));

        return Ok(new
        {
            status = "executed",
            output = "Permission denied: requires elevated privileges",
            exit_code = 1
        });
    }

    /// <summary>
    /// ?? Señuelo: Upload de archivos
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> FakeUpload(IFormFile? file)
    {
        await LogHoneypotActivity("FILE_UPLOAD_ATTEMPT", new
        {
            FileName = file?.FileName,
            ContentType = file?.ContentType,
            Size = file?.Length
        });

        return Ok(new
        {
            status = "uploaded",
            path = "/uploads/" + (file?.FileName ?? "unknown"),
            message = "File queued for processing"
        });
    }

    /// <summary>
    /// ?? Señuelo: SQL Query (SQL Injection trap)
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> FakeQuery([FromBody] FakeQueryRequest request)
    {
        await LogHoneypotActivity("SQL_QUERY_ATTEMPT", new
        {
            Query = request.Query,
            Database = request.Database
        });

        return Ok(new
        {
            status = "error",
            message = "Query execution disabled in production",
            query_received = request.Query?[..Math.Min(50, request.Query?.Length ?? 0)] + "..."
        });
    }

    private async Task LogHoneypotActivity(string activityType, object? additionalData)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var auditLog = new AuditLog
        {
            CorrelationId = Guid.NewGuid().ToString("N")[..12],
            HttpMethod = Request.Method,
            RequestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}",
            RequestPath = Request.Path,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            LocalTimestamp = DateTime.Now,
            StatusCode = 200,
            IsSuccessful = false, // Los honeypots siempre son "fallidos" desde perspectiva del atacante
            ActionType = "HONEYPOT_" + activityType,
            EntityName = "Honeypot",
            RequestBody = additionalData != null ? System.Text.Json.JsonSerializer.Serialize(additionalData) : null,
            ErrorMessage = $"Honeypot triggered: {activityType}",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ServerName = Environment.MachineName
        };

        try
        {
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging honeypot activity");
        }

        _logger.LogWarning(
            "?? HONEYPOT TRIGGERED - Type: {ActivityType}, IP: {Ip}, UserAgent: {UserAgent}",
            activityType, clientIp, userAgent);
    }
}

// DTOs para los endpoints honeypot
public record FakeLoginRequest(string? Username, string? Password);
public record FakeExecuteRequest(string? Command, string? Args);
public record FakeQueryRequest(string? Query, string? Database);
