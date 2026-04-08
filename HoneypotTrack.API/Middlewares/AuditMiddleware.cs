using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Middlewares;

public partial class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AuditMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IWebHostEnvironment env)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var auditLog = new AuditLog
        {
            CorrelationId = correlationId,
            HttpMethod = context.Request.Method,
            RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
            RequestPath = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            Timestamp = DateTime.UtcNow,
            LocalTimestamp = DateTime.Now,
            Environment = env.EnvironmentName,
            ServerName = Environment.MachineName
        };

        // Verificar si debemos interceptar el response
        var shouldCaptureResponse = !ShouldSkipResponseCapture(context.Request.Path, env);
        Stream? originalBodyStream = null;
        MemoryStream? responseBody = null;

        try
        {
            // Capturar informaci�n del cliente
            CaptureClientInfo(context, auditLog);

            // Capturar request body
            await CaptureRequestBody(context, auditLog);

            if (shouldCaptureResponse)
            {
                // Guardar referencia al stream original
                originalBodyStream = context.Response.Body;
                // Crear nuestro buffer para capturar la respuesta
                responseBody = new MemoryStream();
                context.Response.Body = responseBody;
            }

            // Ejecutar el siguiente middleware/controller
            await _next(context);

            // Procesar y enviar el response
            if (shouldCaptureResponse && responseBody != null && originalBodyStream != null)
            {
                // IMPORTANTE: Obtener los bytes ANTES de cualquier otra operaci�n
                var responseBytes = responseBody.ToArray();
                var responseText = Encoding.UTF8.GetString(responseBytes);

                // Guardar en audit log PRIMERO
                if (!string.IsNullOrEmpty(responseText) && responseText.Length <= 10000)
                {
                    auditLog.ResponseBody = responseText;
                }
                else if (!string.IsNullOrEmpty(responseText))
                {
                    auditLog.ResponseBody = "[Response too large - truncated]";
                }
                auditLog.ResponseSize = responseBytes.Length;

                // DESPU�S enviar la respuesta al cliente
                // Restaurar el stream original ANTES de escribir
                context.Response.Body = originalBodyStream;
                
                // Escribir los bytes directamente al stream original
                if (responseBytes.Length > 0)
                {
                    await originalBodyStream.WriteAsync(responseBytes);
                    await originalBodyStream.FlushAsync();
                }
            }

            // Capturar informaci�n de la acci�n
            CaptureActionInfo(context, auditLog);

            auditLog.StatusCode = context.Response.StatusCode;
            auditLog.IsSuccessful = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;
        }
        catch (Exception ex)
        {
            // Si hay una excepci�n y tenemos el stream original, restaurarlo
            if (originalBodyStream != null)
            {
                context.Response.Body = originalBodyStream;
            }
            
            auditLog.StatusCode = 500;
            auditLog.IsSuccessful = false;
            auditLog.ErrorMessage = ex.Message;
            auditLog.ExceptionDetails = ex.ToString();
            _logger.LogError(ex, "Error en la petici�n {CorrelationId}", correlationId);
            throw;
        }
        finally
        {
            // Limpiar el MemoryStream DESPU�S de haber enviado la respuesta
            responseBody?.Dispose();

            stopwatch.Stop();
            auditLog.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            try
            {
                // Solo guardar si no es la ruta de WatchDog o swagger
                if (!ShouldSkipAudit(context.Request.Path))
                {
                    dbContext.AuditLogs.Add(auditLog);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando audit log");
            }
        }
    }

    private static void CaptureClientInfo(HttpContext context, AuditLog auditLog)
    {
        // IP Address
        auditLog.IpAddress = GetClientIpAddress(context);

        // User Agent
        auditLog.UserAgent = context.Request.Headers.UserAgent.ToString();

        // Parsear User Agent
        ParseUserAgent(auditLog.UserAgent, auditLog);

        // MAC Address (si est� disponible en headers personalizados)
        auditLog.MacAddress = context.Request.Headers["X-MAC-Address"].FirstOrDefault()
            ?? GetLocalMacAddress();

        // Referer y Origin
        auditLog.Referer = context.Request.Headers.Referer.ToString();
        auditLog.Origin = context.Request.Headers.Origin.ToString();

        // Session ID
        auditLog.SessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault()
            ?? context.Session?.Id;

        // Usuario (si est� autenticado)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            auditLog.UserName = context.User.Identity.Name;
            auditLog.UserEmail = context.User.Claims
                .FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;

            var userIdClaim = context.User.Claims
                .FirstOrDefault(c => c.Type.Contains("userid", StringComparison.OrdinalIgnoreCase) ||
                                     c.Type.Contains("sub", StringComparison.OrdinalIgnoreCase))?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                auditLog.UserId = userId;
            }
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
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

    private static string? GetLocalMacAddress()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                     n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return nic?.GetPhysicalAddress().ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void ParseUserAgent(string? userAgent, AuditLog auditLog)
    {
        if (string.IsNullOrEmpty(userAgent)) return;

        // Detectar navegador
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Microsoft Edge";
            auditLog.BrowserVersion = ExtractVersion(userAgent, @"Edg/([\d.]+)");
        }
        else if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Google Chrome";
            auditLog.BrowserVersion = ExtractVersion(userAgent, @"Chrome/([\d.]+)");
        }
        else if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Mozilla Firefox";
            auditLog.BrowserVersion = ExtractVersion(userAgent, @"Firefox/([\d.]+)");
        }
        else if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
                 !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Safari";
            auditLog.BrowserVersion = ExtractVersion(userAgent, @"Version/([\d.]+)");
        }
        else if (userAgent.Contains("PostmanRuntime", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Postman";
            auditLog.BrowserVersion = ExtractVersion(userAgent, @"PostmanRuntime/([\d.]+)");
        }
        else if (userAgent.Contains("Insomnia", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "Insomnia";
        }
        else if (userAgent.Contains("curl", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.Browser = "cURL";
        }
        else
        {
            auditLog.Browser = "Unknown";
        }

        // Detectar sistema operativo
        if (userAgent.Contains("Windows NT 10", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "Windows 10/11";
        }
        else if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "Windows";
        }
        else if (userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "macOS";
        }
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "Linux";
        }
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "Android";
        }
        else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.OperatingSystem = "iOS";
        }

        // Detectar tipo de dispositivo
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.DeviceType = "Mobile";
        }
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.DeviceType = "Tablet";
        }
        else if (userAgent.Contains("PostmanRuntime", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("Insomnia", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("curl", StringComparison.OrdinalIgnoreCase))
        {
            auditLog.DeviceType = "API Client";
        }
        else
        {
            auditLog.DeviceType = "Desktop";
        }
    }

    private static string? ExtractVersion(string userAgent, string pattern)
    {
        var match = Regex.Match(userAgent, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task CaptureRequestBody(HttpContext context, AuditLog auditLog)
    {
        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            auditLog.RequestBody = SanitizeBody(body);
            auditLog.RequestSize = Encoding.UTF8.GetByteCount(body);
        }
    }

    private static async Task CaptureResponseBody(HttpContext context, MemoryStream responseBody,
        Stream originalBodyStream, AuditLog auditLog)
    {
        try
        {
            // Verificar si el stream est� disponible antes de usarlo
            if (!responseBody.CanSeek || !responseBody.CanRead)
            {
                // El stream fue cerrado por otro middleware, restaurar el body original
                context.Response.Body = originalBodyStream;
                auditLog.ResponseBody = "[Response body not available - stream was closed]";
                return;
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            
            // IMPORTANTE: Copiar al stream original y hacer flush
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            await originalBodyStream.FlushAsync();
            
            // Restaurar el body original
            context.Response.Body = originalBodyStream;

            // Guardar en audit log
            if (responseText.Length <= 10000)
            {
                auditLog.ResponseBody = responseText;
            }
            else
            {
                auditLog.ResponseBody = "[Response too large - truncated]";
            }

            auditLog.ResponseSize = Encoding.UTF8.GetByteCount(responseText);
        }
        catch (ObjectDisposedException)
        {
            // El stream fue disposed por otro middleware
            context.Response.Body = originalBodyStream;
            auditLog.ResponseBody = "[Response body not available - stream was disposed]";
        }
    }

    private static async Task CaptureResponseBodyForAuditOnly(MemoryStream responseBody, AuditLog auditLog)
    {
        try
        {
            // Solo leer para auditor�a - el response ya fue enviado por otro middleware
            if (!responseBody.CanSeek || !responseBody.CanRead)
            {
                auditLog.ResponseBody = "[Response captured by another middleware]";
                return;
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
            var responseText = await reader.ReadToEndAsync();

            if (responseText.Length <= 10000)
            {
                auditLog.ResponseBody = responseText;
            }
            else
            {
                auditLog.ResponseBody = "[Response too large - truncated]";
            }

            auditLog.ResponseSize = Encoding.UTF8.GetByteCount(responseText);
        }
        catch (ObjectDisposedException)
        {
            auditLog.ResponseBody = "[Response body disposed by another middleware]";
        }
    }

    private static void CaptureActionInfo(HttpContext context, AuditLog auditLog)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method.ToUpper();

        // Determinar tipo de acci�n
        auditLog.ActionType = method switch
        {
            "GET" => "READ",
            "POST" => "CREATE",
            "PUT" or "PATCH" => "UPDATE",
            "DELETE" => "DELETE",
            _ => method
        };

        // Extraer nombre de entidad del path
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0] == "api")
        {
            auditLog.EntityName = segments[1];

            // Extraer ID si existe
            if (segments.Length >= 3 && int.TryParse(segments[2], out _))
            {
                auditLog.EntityId = segments[2];
            }
        }
    }

    private static string SanitizeBody(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;

        try
        {
            // Ocultar campos sensibles
            var sensitiveFields = new[] { "password", "token", "secret", "apikey", "authorization" };
            var json = JsonDocument.Parse(body);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            SanitizeJsonElement(json.RootElement, writer, sensitiveFields);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return body;
        }
    }

    private static void SanitizeJsonElement(JsonElement element, Utf8JsonWriter writer, string[] sensitiveFields)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (sensitiveFields.Any(f => property.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WriteStringValue("***HIDDEN***");
                    }
                    else
                    {
                        SanitizeJsonElement(property.Value, writer, sensitiveFields);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    SanitizeJsonElement(item, writer, sensitiveFields);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool ShouldSkipAudit(string path)
    {
        var skipPaths = new[]
        {
            "/watchdog",
            "/swagger",
            "/health",
            "/favicon.ico",
            "/_framework",
            "/_blazor"
        };

        return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldSkipResponseCapture(string path, IWebHostEnvironment env)
    {
        // En desarrollo, algunas rutas son manejadas por middlewares de VS que cierran el stream
        var skipPaths = new[]
        {
            "/watchdog",
            "/swagger",
            "/health",
            "/favicon.ico",
            "/_framework",
            "/_blazor"
        };

        return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditMiddleware>();
    }
}
