using System.Collections.Concurrent;

namespace HoneypotTrack.API.Security;

/// <summary>
/// ?? Sistema de Alertas de Seguridad
/// </summary>
public class SecurityAlertService : ISecurityAlertService
{
    private readonly ILogger<SecurityAlertService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentQueue<SecurityAlert> _recentAlerts = new();
    private const int MaxRecentAlerts = 100;

    public event EventHandler<SecurityAlert>? OnAlertRaised;

    public SecurityAlertService(ILogger<SecurityAlertService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Genera una alerta de seguridad
    /// </summary>
    public async Task RaiseAlertAsync(SecurityAlert alert)
    {
        // Agregar timestamp si no tiene
        if (alert.Timestamp == default)
        {
            alert = alert with { Timestamp = DateTime.UtcNow };
        }

        // Almacenar en memoria
        _recentAlerts.Enqueue(alert);
        while (_recentAlerts.Count > MaxRecentAlerts)
        {
            _recentAlerts.TryDequeue(out _);
        }

        // Log de la alerta
        LogAlert(alert);

        // Disparar evento
        OnAlertRaised?.Invoke(this, alert);

        // Enviar notificaciones según configuración
        await SendNotificationsAsync(alert);
    }

    /// <summary>
    /// Obtiene las alertas recientes
    /// </summary>
    public IEnumerable<SecurityAlert> GetRecentAlerts(int count = 20)
    {
        return _recentAlerts
            .OrderByDescending(a => a.Timestamp)
            .Take(count);
    }

    /// <summary>
    /// Obtiene alertas por nivel de severidad
    /// </summary>
    public IEnumerable<SecurityAlert> GetAlertsBySeverity(AlertSeverity minSeverity)
    {
        return _recentAlerts
            .Where(a => a.Severity >= minSeverity)
            .OrderByDescending(a => a.Timestamp);
    }

    private void LogAlert(SecurityAlert alert)
    {
        var emoji = alert.Severity switch
        {
            AlertSeverity.Critical => "??",
            AlertSeverity.High => "??",
            AlertSeverity.Medium => "?",
            AlertSeverity.Low => "??",
            _ => "??"
        };

        var logMessage = $"{emoji} SECURITY ALERT [{alert.Severity}] - {alert.Title}: {alert.Message}";

        switch (alert.Severity)
        {
            case AlertSeverity.Critical:
                _logger.LogCritical(logMessage);
                break;
            case AlertSeverity.High:
                _logger.LogError(logMessage);
                break;
            case AlertSeverity.Medium:
                _logger.LogWarning(logMessage);
                break;
            default:
                _logger.LogInformation(logMessage);
                break;
        }
    }

    private async Task SendNotificationsAsync(SecurityAlert alert)
    {
        var tasks = new List<Task>();

        // Solo enviar notificaciones externas para alertas de severidad alta o crítica
        if (alert.Severity < AlertSeverity.High)
        {
            return;
        }

        // Email notification
        var emailEnabled = _configuration.GetValue<bool>("Alerts:Email:Enabled");
        if (emailEnabled)
        {
            tasks.Add(SendEmailAlertAsync(alert));
        }

        // Webhook notification (Discord, Slack, Teams, etc.)
        var webhookUrl = _configuration["Alerts:Webhook:Url"];
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            tasks.Add(SendWebhookAlertAsync(alert, webhookUrl));
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendEmailAlertAsync(SecurityAlert alert)
    {
        try
        {
            // TODO: Implementar envío de email
            // Por ahora solo logueamos
            _logger.LogInformation("?? Email alert would be sent: {Title}", alert.Title);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email alert");
        }
    }

    private async Task SendWebhookAlertAsync(SecurityAlert alert, string webhookUrl)
    {
        try
        {
            using var client = new HttpClient();
            
            // Formato compatible con Discord/Slack
            var payload = new
            {
                content = $"?? **Security Alert**",
                embeds = new[]
                {
                    new
                    {
                        title = alert.Title,
                        description = alert.Message,
                        color = alert.Severity switch
                        {
                            AlertSeverity.Critical => 15158332, // Rojo
                            AlertSeverity.High => 15105570,     // Naranja
                            AlertSeverity.Medium => 16776960,   // Amarillo
                            _ => 3447003                        // Azul
                        },
                        fields = new[]
                        {
                            new { name = "Severity", value = alert.Severity.ToString(), inline = true },
                            new { name = "Type", value = alert.AlertType ?? "Unknown", inline = true },
                            new { name = "IP", value = alert.SourceIp ?? "N/A", inline = true },
                            new { name = "Timestamp", value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline = false }
                        },
                        timestamp = alert.Timestamp.ToString("o")
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            await client.PostAsync(webhookUrl, content);
            
            _logger.LogInformation("?? Webhook alert sent to: {Url}", webhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook alert");
        }
    }
}

/// <summary>
/// Interfaz del servicio de alertas
/// </summary>
public interface ISecurityAlertService
{
    Task RaiseAlertAsync(SecurityAlert alert);
    IEnumerable<SecurityAlert> GetRecentAlerts(int count = 20);
    IEnumerable<SecurityAlert> GetAlertsBySeverity(AlertSeverity minSeverity);
    event EventHandler<SecurityAlert>? OnAlertRaised;
}

/// <summary>
/// Modelo de alerta de seguridad
/// </summary>
public record SecurityAlert
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public AlertSeverity Severity { get; init; }
    public string? AlertType { get; init; }
    public string? Title { get; init; }
    public string? Message { get; init; }
    public string? SourceIp { get; init; }
    public string? UserAgent { get; init; }
    public string? Endpoint { get; init; }
    public Dictionary<string, object>? AdditionalData { get; init; }
}

/// <summary>
/// Niveles de severidad de alertas
/// </summary>
public enum AlertSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
