using System.Text;
using System.Text.Json;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Security;

/// <summary>
/// Middleware de seguridad que detecta y registra amenazas OWASP Top 10
/// Integrado con sistema Honeypot para atrapar atacantes
/// </summary>
public class SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, IConfiguration configuration, ISecurityAlertService alertService)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<SecurityMiddleware> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly ISecurityAlertService _alertService = alertService;

    // IPs en whitelist que nunca ser�n bloqueadas (localhost, tu IP, etc.)
    private static readonly HashSet<string> _whitelistedIps =
    [
        "127.0.0.1",
        "::1",
        "localhost"
    ];

    // Endpoints que activan el honeypot (en lugar de bloquear)
    private static readonly string[] _honeypotTriggerEndpoints =
    [
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/forgot-password"
    ];

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IHoneypotSessionService honeypotService)
    {
        var clientIp = GetClientIp(context);
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        // En desarrollo o si la IP est� en whitelist, no bloquear
        var isWhitelisted = IsWhitelisted(clientIp, isDevelopment);
        var allowWhitelistHoneypotActivation =
            _configuration.GetValue<bool?>("Security:Honeypot:AllowWhitelistActivation") ?? false;
        var issueSessionOnAnyHighSeverityThreat =
            _configuration.GetValue<bool?>("Security:Honeypot:IssueSessionOnAnyHighSeverityThreat") ?? false;

        // Analizar la petici�n en busca de amenazas
        var simulatedThreat = GetSimulatedThreatForTesting(context, isDevelopment);
        var detectedThreats = (await AnalyzeRequest(context)).Where(t => t.IsThreatDetected).ToList();
        var isSimulatedThreat = simulatedThreat?.IsThreatDetected == true;
        if (isSimulatedThreat)
        {
            detectedThreats.Insert(0, simulatedThreat!);
        }

        if (detectedThreats.Any())
        {
            var primaryThreat = detectedThreats.First();

            // Registrar las amenazas (SIEMPRE, incluso en whitelist para auditor�a)
            foreach (var threat in detectedThreats)
            {
                await LogSecurityThreat(context, dbContext, threat, clientIp);
                await RaiseSecurityAlert(threat, clientIp, context);
            }

            // ?? HONEYPOT: Si es un endpoint de autenticaci�n, crear sesi�n trampa
            var requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var isAuthEndpoint = _honeypotTriggerEndpoints.Any(e => requestPath.Contains(e.ToLowerInvariant()));
            var shouldIssueSessionForThreat =
                (isSimulatedThreat || !isWhitelisted || allowWhitelistHoneypotActivation) &&
                primaryThreat.Severity >= 7 &&
                (isAuthEndpoint || issueSessionOnAnyHighSeverityThreat);

            if (shouldIssueSessionForThreat)
            {
                _logger.LogWarning(
                    "?? ACTIVANDO HONEYPOT - Amenaza {ThreatType} en {Path} desde IP: {Ip}",
                    primaryThreat.ThreatType, requestPath, clientIp);

                // Obtener el payload original
                string? payload = null;
                if (context.Request.ContentLength > 0)
                {
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    payload = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                // Crear sesi�n honeypot
                var honeypotResult = await honeypotService.CreateHoneypotSessionAsync(
                    clientIp ?? "unknown",
                    context.Request.Headers.UserAgent.ToString(),
                    primaryThreat.ThreatType ?? "UNKNOWN",
                    payload
                );

                if (honeypotResult.Success)
                {
                    // Devolver respuesta de "login exitoso" falsa
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        isSuccess = true,
                        message = isAuthEndpoint ? "Login exitoso" : "Acceso concedido",
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
                            },
                            sessionType = isAuthEndpoint ? "honeypot-auth" : "honeypot-threat"
                        }
                    });
                    return; // No continuar al controller real
                }

                if (isDevelopment)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        isSuccess = false,
                        message = "Se detectó una amenaza, pero falló la activación del honeypot.",
                        detail = honeypotResult.FailureReason ?? "Razón no disponible"
                    });
                    return;
                }
            }

            _logger.LogWarning("?? AMENAZA DETECTADA (sin bloqueo de IP): {Ip} - {ThreatType}",
                clientIp, detectedThreats.First().ThreatType);

            // Agregar header indicando detecci�n de amenaza
            context.Response.Headers["X-Security-Warning"] = "Suspicious activity detected";
        }

        if (ShouldForceHoneypotForTesting(context, isDevelopment) &&
            IsAuthEndpoint(context.Request.Path.Value))
        {
            _logger.LogWarning("TEST HONEYPOT activado manualmente desde IP: {Ip}", clientIp);

            string? payload = null;
            if (context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                payload = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var honeypotResult = await honeypotService.CreateHoneypotSessionAsync(
                clientIp ?? "unknown",
                context.Request.Headers.UserAgent.ToString(),
                "TEST_TRIGGER",
                payload
            );

            if (honeypotResult.Success)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
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
                return;
            }

            if (isDevelopment)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    isSuccess = false,
                    message = "Falló la activación manual del honeypot.",
                    detail = honeypotResult.FailureReason ?? "Razón no disponible"
                });
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// ?? Genera una alerta de seguridad
    /// </summary>
    private async Task RaiseSecurityAlert(SecurityThreatDetector.ThreatAnalysisResult threat, string? clientIp, HttpContext context)
    {
        var severity = threat.Severity switch
        {
            >= 9 => AlertSeverity.Critical,
            >= 7 => AlertSeverity.High,
            >= 5 => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };

        var alert = new SecurityAlert
        {
            Severity = severity,
            AlertType = threat.ThreatType,
            Title = $"Amenaza detectada: {threat.ThreatType}",
            Message = threat.Description ?? "Se ha detectado actividad maliciosa",
            SourceIp = clientIp,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Endpoint = context.Request.Path,
            AdditionalData = new Dictionary<string, object>
            {
                ["Category"] = threat.ThreatCategory ?? "Unknown",
                ["Pattern"] = threat.MatchedPattern ?? "Unknown",
                ["Severity"] = threat.Severity
            }
        };

        await _alertService.RaiseAlertAsync(alert);
    }

    /// <summary>
    /// Verifica si una IP est� en la whitelist
    /// </summary>
    private bool IsWhitelisted(string? ip, bool isDevelopment)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        
        // En desarrollo, localhost siempre est� en whitelist
        if (isDevelopment && (ip == "127.0.0.1" || ip == "::1" || ip.StartsWith("192.168.") || ip.StartsWith("10.")))
        {
            return true;
        }
        
        // IPs configuradas en appsettings
        var configuredWhitelist = _configuration.GetSection("Security:WhitelistedIps").Get<string[]>() ?? [];
        
        return _whitelistedIps.Contains(ip) || configuredWhitelist.Contains(ip);
    }

    /// <summary>
    /// Agrega una IP a la whitelist en tiempo de ejecuci�n
    /// </summary>
    public static void AddToWhitelist(string ip)
    {
        _whitelistedIps.Add(ip);
    }

    /// <summary>
    /// Remueve una IP de la whitelist
    /// </summary>
    public static void RemoveFromWhitelist(string ip)
    {
        _whitelistedIps.Remove(ip);
    }

    /// <summary>
    /// Compatibilidad con el dashboard: el bloqueo de IPs fue deshabilitado.
    /// </summary>
    public static bool UnblockIp(string ip) => false;

    /// <summary>
    /// Compatibilidad con el dashboard: ya no se mantienen IPs bloqueadas en memoria.
    /// </summary>
    public static IReadOnlyDictionary<string, BlockedIpInfo> GetBlockedIps() =>
        new Dictionary<string, BlockedIpInfo>();

    private async Task<List<SecurityThreatDetector.ThreatAnalysisResult>> AnalyzeRequest(HttpContext context)
    {
        var threats = new List<SecurityThreatDetector.ThreatAnalysisResult>();
        var fieldsToAnalyze = new Dictionary<string, string?>();
        var requestPath = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var isAuthEndpoint = _honeypotTriggerEndpoints.Any(e => requestPath.Contains(e));

        // Analizar URL y Query String
        fieldsToAnalyze["URL"] = context.Request.Path.Value;
        fieldsToAnalyze["QueryString"] = context.Request.QueryString.Value;

        // Analizar Headers sospechosos
        foreach (var header in context.Request.Headers)
        {
            if (ShouldAnalyzeHeader(header.Key, isAuthEndpoint))
            {
                fieldsToAnalyze[$"Header:{header.Key}"] = header.Value.ToString();
            }
        }

        // Analizar Request Body (si es JSON)
        if (context.Request.ContentType?.Contains("application/json") == true && 
            context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                // Analizar campos individuales del JSON
                try
                {
                    var jsonFields = ExtractJsonFields(body);
                    foreach (var field in jsonFields)
                    {
                        if (ShouldSkipFieldAnalysis(field.Key, isAuthEndpoint))
                        {
                            continue;
                        }

                        fieldsToAnalyze[$"Body:{field.Key}"] = field.Value;
                    }
                }
                catch
                {
                    // Si no es JSON v�lido, analizar como texto plano
                    fieldsToAnalyze["Body:raw"] = body;
                }
            }
        }

        // Analizar todos los campos
        threats.AddRange(SecurityThreatDetector.AnalyzeMultipleFields(fieldsToAnalyze));

        return threats;
    }

    private bool ShouldForceHoneypotForTesting(HttpContext context, bool isDevelopment)
    {
        var testHeaderEnabled = _configuration.GetValue<bool?>("Security:Honeypot:EnableTestHeaderInDevelopment") ?? true;
        if (!isDevelopment || !testHeaderEnabled)
        {
            return false;
        }

        var headerName = _configuration["Security:Honeypot:TestHeaderName"] ?? "X-Test-Honeypot";
        var expectedValue = _configuration["Security:Honeypot:TestHeaderValue"] ?? "true";

        if (!context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return false;
        }

        return string.Equals(headerValue.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private SecurityThreatDetector.ThreatAnalysisResult? GetSimulatedThreatForTesting(HttpContext context, bool isDevelopment)
    {
        var simulationEnabled = _configuration.GetValue<bool?>("Security:Honeypot:EnableThreatSimulationInDevelopment") ?? true;
        if (!isDevelopment || !simulationEnabled)
        {
            return null;
        }

        var headerName = _configuration["Security:Honeypot:ThreatSimulationHeaderName"] ?? "X-Simulate-Threat";
        if (!context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return null;
        }

        return headerValue.ToString().Trim().ToUpperInvariant() switch
        {
            "SQL_INJECTION" => new SecurityThreatDetector.ThreatAnalysisResult
            {
                IsThreatDetected = true,
                ThreatType = "SQL_INJECTION",
                ThreatCategory = "SIMULATED_THREAT",
                Description = "[Simulated] Detección de SQL Injection en desarrollo",
                MatchedPattern = "SIMULATED:SQL_INJECTION",
                Severity = 9
            },
            _ => null
        };
    }

    private static bool IsAuthEndpoint(string? requestPath)
    {
        var normalizedPath = requestPath?.ToLowerInvariant() ?? string.Empty;
        return _honeypotTriggerEndpoints.Any(e => normalizedPath.Contains(e));
    }

    private static bool ShouldSkipFieldAnalysis(string fieldName, bool isAuthEndpoint)
    {
        if (!isAuthEndpoint)
        {
            return false;
        }

        // Las contraseñas y tokens suelen incluir caracteres especiales válidos como $, ;, &, (, ), *
        // que disparan falsos positivos con reglas genéricas de command/LDAP injection.
        var sensitiveAuthFields = new[]
        {
            "password",
            "confirmPassword",
            "currentPassword",
            "newPassword",
            "refreshToken",
            "token"
        };

        return sensitiveAuthFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string?> ExtractJsonFields(string json, string prefix = "")
    {
        var fields = new Dictionary<string, string?>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            ExtractJsonFieldsRecursive(doc.RootElement, fields, prefix);
        }
        catch
        {
            // Ignorar errores de parsing
        }

        return fields;
    }

    private static void ExtractJsonFieldsRecursive(JsonElement element, Dictionary<string, string?> fields, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    ExtractJsonFieldsRecursive(property.Value, fields, key);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ExtractJsonFieldsRecursive(item, fields, $"{prefix}[{index}]");
                    index++;
                }
                break;

            case JsonValueKind.String:
                fields[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
                fields[prefix] = element.GetRawText();
                break;
        }
    }

    private static bool ShouldAnalyzeHeader(string headerName, bool isAuthEndpoint)
    {
        if (isAuthEndpoint)
        {
            // En auth estos headers suelen contener localhost/swagger y generan falsos positivos
            // sin aportar valor para credenciales legítimas.
            var noisyAuthHeaders = new[]
            {
                "Referer",
                "User-Agent",
                "Cookie"
            };

            if (noisyAuthHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Headers que pueden contener payloads maliciosos
        var suspiciousHeaders = new[]
        {
            "X-Forwarded-For",
            "X-Forwarded-Host",
            "X-Original-URL",
            "X-Rewrite-URL",
            "Referer",
            "User-Agent",
            "Cookie"
        };

        return suspiciousHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    private async Task LogSecurityThreat(HttpContext context, AppDbContext dbContext,
        SecurityThreatDetector.ThreatAnalysisResult threat, string? clientIp)
    {
        var securityLog = new AuditLog
        {
            CorrelationId = context.Response.Headers["X-Correlation-Id"].ToString(),
            HttpMethod = context.Request.Method,
            RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
            RequestPath = context.Request.Path,
            IpAddress = clientIp,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow,
            LocalTimestamp = DateTime.Now,
            StatusCode = 0, // Ser� actualizado despu�s
            IsSuccessful = false,
            ActionType = "SECURITY_THREAT",
            EntityName = threat.ThreatType,
            ErrorMessage = $"[{threat.ThreatCategory}] {threat.Description}",
            ExceptionDetails = $"Pattern: {threat.MatchedPattern}, Severity: {threat.Severity}/10",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ServerName = Environment.MachineName
        };

        try
        {
            dbContext.AuditLogs.Add(securityLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando log de seguridad");
        }

        _logger.LogWarning(
            "?? AMENAZA DETECTADA - Tipo: {ThreatType}, Categor�a: {Category}, IP: {Ip}, Severidad: {Severity}/10",
            threat.ThreatType,
            threat.ThreatCategory,
            clientIp,
            threat.Severity);
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public record BlockedIpInfo
    {
        public DateTime BlockedUntil { get; init; }
        public string? Reason { get; init; }
        public int ThreatCount { get; init; }
    }

}

/// <summary>
/// Extension methods para SecurityMiddleware
/// </summary>
public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMiddleware>();
    }
}
