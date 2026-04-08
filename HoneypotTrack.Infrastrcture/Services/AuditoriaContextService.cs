using Microsoft.AspNetCore.Http;
using HoneypotTrack.Infrastrcture.Persistences.Interfaces;

namespace HoneypotTrack.Infrastrcture.Services;

public class AuditoriaContextService : IAuditoriaContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditoriaContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetUsuarioId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true) return null;

        var userIdClaim = context.User.Claims
            .FirstOrDefault(c => c.Type.Contains("userid", StringComparison.OrdinalIgnoreCase) ||
                                 c.Type.Contains("sub", StringComparison.OrdinalIgnoreCase) ||
                                 c.Type.Contains("nameid", StringComparison.OrdinalIgnoreCase))?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public string? GetUsuarioNombre()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User.Identity?.Name;
    }

    public string? GetUsuarioEmail()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.User.Claims
            .FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    public string? GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Verificar headers de proxy
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetPath()
    {
        return _httpContextAccessor.HttpContext?.Request.Path.Value;
    }

    public string? GetMetodoHttp()
    {
        return _httpContextAccessor.HttpContext?.Request.Method;
    }

    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
    }

    public string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Response.Headers["X-Correlation-Id"].FirstOrDefault() 
            ?? context?.TraceIdentifier;
    }
}
