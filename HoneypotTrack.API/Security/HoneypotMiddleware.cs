using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Security;

/// <summary>
/// Middleware que detecta sesiones honeypot y registra toda la actividad del atacante
/// </summary>
public class HoneypotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HoneypotMiddleware> _logger;

    public HoneypotMiddleware(RequestDelegate next, ILogger<HoneypotMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IHoneypotSessionService honeypotService,
        AppDbContext mainContext)
    {
        // Verificar si hay token en el header
        var authHeader = context.Request.Headers.Authorization.ToString();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            
            // Verificar si es un token honeypot
            if (honeypotService.IsHoneypotToken(token))
            {
                // Marcar el request como honeypot
                context.Items["IsHoneypot"] = true;
                context.Items["HoneypotToken"] = token;
                
                // Actualizar actividad de la sesión
                await honeypotService.UpdateSessionActivityAsync(token);
                
                // Log detallado de la actividad del atacante
                await LogAttackerActivity(context, mainContext, token);
                
                _logger.LogWarning(
                    "🍯 Actividad Honeypot - {Method} {Path} - IP: {Ip}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);
            }
        }

        await _next(context);
    }

    private async Task LogAttackerActivity(HttpContext context, AppDbContext mainContext, string token)
    {
        try
        {
            // Leer el body si existe
            string? requestBody = null;
            if (context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var auditLog = new AuditLog
            {
                HttpMethod = context.Request.Method,
                RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                RequestPath = context.Request.Path,
                QueryString = context.Request.QueryString.Value,
                RequestBody = SanitizeBody(requestBody),
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                ActionType = "HONEYPOT_ACTIVITY",
                EntityName = "AttackerAction",
                Timestamp = DateTime.UtcNow,
                LocalTimestamp = DateTime.Now,
                IsSuccessful = true,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                ServerName = Environment.MachineName,
                Referer = context.Request.Headers.Referer.ToString(),
                Origin = context.Request.Headers.Origin.ToString(),
                SessionId = token[^20..] // Últimos 20 caracteres del token como ID
            };

            mainContext.AuditLogs.Add(auditLog);
            await mainContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando actividad honeypot");
        }
    }

    private static string? SanitizeBody(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        
        // Limitar tamaño
        if (body.Length > 5000)
            body = body[..5000] + "...[TRUNCATED]";
        
        return body;
    }
}

/// <summary>
/// Extension method para registrar el middleware
/// </summary>
public static class HoneypotMiddlewareExtensions
{
    public static IApplicationBuilder UseHoneypotMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HoneypotMiddleware>();
    }
}
