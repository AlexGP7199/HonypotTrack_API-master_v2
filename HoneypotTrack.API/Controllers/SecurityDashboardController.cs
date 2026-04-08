using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoneypotTrack.Infrastrcture.Persistences.Context;
using HoneypotTrack.API.Security;

namespace HoneypotTrack.API.Controllers;

/// <summary>
/// ?? Dashboard de Seguridad y Monitoreo del HoneyPot
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SecurityDashboardController(AppDbContext dbContext, ILogger<SecurityDashboardController> logger, ISecurityAlertService alertService) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<SecurityDashboardController> _logger = logger;
    private readonly ISecurityAlertService _alertService = alertService;

    /// <summary>
    /// ?? Obtiene estad�sticas generales de seguridad
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(SecurityStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecurityStats([FromQuery] int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var threatLogs = await _dbContext.AuditLogs
            .Where(a => a.Timestamp >= startDate && 
                       (a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_")))
            .ToListAsync();

        var stats = new SecurityStatsResponse
        {
            Period = $"Last {days} days",
            TotalThreats = threatLogs.Count(a => a.ActionType == "SECURITY_THREAT"),
            TotalHoneypotTriggers = threatLogs.Count(a => a.ActionType!.StartsWith("HONEYPOT_")),
            UniqueAttackerIPs = threatLogs.Select(a => a.IpAddress).Distinct().Count(),
            ThreatsByType = threatLogs
                .Where(a => a.ActionType == "SECURITY_THREAT")
                .GroupBy(a => a.EntityName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            HoneypotTriggersByType = threatLogs
                .Where(a => a.ActionType!.StartsWith("HONEYPOT_"))
                .GroupBy(a => a.ActionType!.Replace("HONEYPOT_", ""))
                .ToDictionary(g => g.Key, g => g.Count()),
            ThreatsByDay = threatLogs
                .GroupBy(a => a.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
            TopAttackerIPs = threatLogs
                .GroupBy(a => a.IpAddress ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new AttackerIpSummary
                {
                    IpAddress = g.Key,
                    TotalAttempts = g.Count(),
                    ThreatTypes = g.Select(a => a.EntityName ?? a.ActionType).Distinct().ToList(),
                    FirstSeen = g.Min(a => a.Timestamp),
                    LastSeen = g.Max(a => a.Timestamp)
                })
                .ToList(),
            BlockedIPs = SecurityMiddleware.GetBlockedIps()
                .Select(b => new BlockedIpSummary
                {
                    IpAddress = b.Key,
                    BlockedUntil = b.Value.BlockedUntil,
                    Reason = b.Value.Reason,
                    ThreatCount = b.Value.ThreatCount
                })
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// ?? Obtiene el perfil detallado de un atacante por IP
    /// </summary>
    [HttpGet("attacker/{ip}")]
    [ProducesResponseType(typeof(AttackerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttackerProfile(string ip)
    {
        var attackerLogs = await _dbContext.AuditLogs
            .Where(a => a.IpAddress == ip && 
                       (a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_")))
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        if (!attackerLogs.Any())
        {
            return NotFound(new { message = "No malicious activity found for this IP" });
        }

        var profile = new AttackerProfileResponse
        {
            IpAddress = ip,
            TotalAttempts = attackerLogs.Count,
            FirstSeen = attackerLogs.Min(a => a.Timestamp),
            LastSeen = attackerLogs.Max(a => a.Timestamp),
            IsCurrentlyBlocked = SecurityMiddleware.GetBlockedIps().ContainsKey(ip),
            UserAgents = attackerLogs.Select(a => a.UserAgent).Where(ua => !string.IsNullOrEmpty(ua)).Distinct().ToList(),
            Browsers = attackerLogs.Select(a => a.Browser).Where(b => !string.IsNullOrEmpty(b)).Distinct().ToList(),
            OperatingSystems = attackerLogs.Select(a => a.OperatingSystem).Where(os => !string.IsNullOrEmpty(os)).Distinct().ToList(),
            ThreatTypes = attackerLogs
                .GroupBy(a => a.EntityName ?? a.ActionType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            TargetedEndpoints = attackerLogs
                .GroupBy(a => a.RequestPath ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count()),
            Timeline = attackerLogs
                .Take(50)
                .Select(a => new AttackTimelineEntry
                {
                    Timestamp = a.Timestamp,
                    ActionType = a.ActionType,
                    ThreatType = a.EntityName,
                    Endpoint = a.RequestPath,
                    Description = a.ErrorMessage,
                    Severity = ExtractSeverity(a.ExceptionDetails)
                })
                .ToList(),
            RiskScore = CalculateRiskScore(attackerLogs)
        };

        return Ok(profile);
    }

    /// <summary>
    /// ?? Lista todos los ataques recientes
    /// </summary>
    [HttpGet("attacks")]
    [ProducesResponseType(typeof(AttackListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentAttacks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? threatType = null,
        [FromQuery] string? ip = null)
    {
        var query = _dbContext.AuditLogs
            .Where(a => a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_"));

        if (!string.IsNullOrEmpty(threatType))
        {
            query = query.Where(a => a.EntityName == threatType || a.ActionType!.Contains(threatType));
        }

        if (!string.IsNullOrEmpty(ip))
        {
            query = query.Where(a => a.IpAddress == ip);
        }

        var totalCount = await query.CountAsync();

        var attacks = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttackEntry
            {
                Id = a.AuditLogId,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress,
                ActionType = a.ActionType,
                ThreatType = a.EntityName,
                Endpoint = a.RequestPath,
                Method = a.HttpMethod,
                Description = a.ErrorMessage,
                UserAgent = a.UserAgent,
                Browser = a.Browser,
                OperatingSystem = a.OperatingSystem
            })
            .ToListAsync();

        return Ok(new AttackListResponse
        {
            Attacks = attacks,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// ?? Obtiene la lista de IPs bloqueadas actualmente
    /// </summary>
    [HttpGet("blocked-ips")]
    [ProducesResponseType(typeof(IEnumerable<BlockedIpSummary>), StatusCodes.Status200OK)]
    public IActionResult GetBlockedIps()
    {
        var blockedIps = SecurityMiddleware.GetBlockedIps()
            .Select(b => new BlockedIpSummary
            {
                IpAddress = b.Key,
                BlockedUntil = b.Value.BlockedUntil,
                Reason = b.Value.Reason,
                ThreatCount = b.Value.ThreatCount,
                RemainingMinutes = (int)(b.Value.BlockedUntil - DateTime.UtcNow).TotalMinutes
            })
            .Where(b => b.RemainingMinutes > 0)
            .ToList();

        return Ok(blockedIps);
    }

    /// <summary>
    /// ?? Desbloquea una IP manualmente
    /// </summary>
    [HttpDelete("blocked-ips/{ip}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UnblockIp(string ip)
    {
        var result = SecurityMiddleware.UnblockIp(ip);

        if (result)
        {
            _logger.LogInformation("IP {Ip} desbloqueada manualmente", ip);
            return Ok(new { message = $"IP {ip} has been unblocked" });
        }

        return NotFound(new { message = $"IP {ip} was not found in blocked list" });
    }

    /// <summary>
    /// ? Agrega una IP a la whitelist
    /// </summary>
    [HttpPost("whitelist/{ip}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult AddToWhitelist(string ip)
    {
        SecurityMiddleware.AddToWhitelist(ip);
        _logger.LogInformation("IP {Ip} agregada a whitelist", ip);
        return Ok(new { message = $"IP {ip} has been added to whitelist" });
    }

    /// <summary>
    /// ?? Obtiene m�tricas en tiempo real
    /// </summary>
    [HttpGet("realtime")]
    [ProducesResponseType(typeof(RealtimeMetrics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRealtimeMetrics()
    {
        var lastHour = DateTime.UtcNow.AddHours(-1);
        var last24Hours = DateTime.UtcNow.AddHours(-24);

        var recentThreats = await _dbContext.AuditLogs
            .Where(a => a.Timestamp >= lastHour && 
                       (a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_")))
            .CountAsync();

        var threatsLast24h = await _dbContext.AuditLogs
            .Where(a => a.Timestamp >= last24Hours && 
                       (a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_")))
            .CountAsync();

        var latestThreat = await _dbContext.AuditLogs
            .Where(a => a.ActionType == "SECURITY_THREAT" || a.ActionType!.StartsWith("HONEYPOT_"))
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        return Ok(new RealtimeMetrics
        {
            ThreatsLastHour = recentThreats,
            ThreatsLast24Hours = threatsLast24h,
            CurrentlyBlockedIPs = SecurityMiddleware.GetBlockedIps().Count,
            LastThreatTimestamp = latestThreat?.Timestamp,
            LastThreatType = latestThreat?.EntityName ?? latestThreat?.ActionType,
            LastThreatIP = latestThreat?.IpAddress,
            SystemStatus = recentThreats > 10 ? "HIGH_ALERT" : recentThreats > 5 ? "WARNING" : "NORMAL"
        });
    }

    /// <summary>
    /// ?? Obtiene las alertas de seguridad recientes (en memoria)
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IEnumerable<SecurityAlert>), StatusCodes.Status200OK)]
    public IActionResult GetRecentAlerts([FromQuery] int count = 20, [FromQuery] string? minSeverity = null)
    {
        IEnumerable<SecurityAlert> alerts;

        if (!string.IsNullOrEmpty(minSeverity) && Enum.TryParse<AlertSeverity>(minSeverity, true, out var severity))
        {
            alerts = _alertService.GetAlertsBySeverity(severity).Take(count);
        }
        else
        {
            alerts = _alertService.GetRecentAlerts(count);
        }

        return Ok(alerts);
    }

    #region Helper Methods

    private static int ExtractSeverity(string? exceptionDetails)
    {
        if (string.IsNullOrEmpty(exceptionDetails)) return 0;

        var match = System.Text.RegularExpressions.Regex.Match(exceptionDetails, @"Severity:\s*(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var severity) ? severity : 0;
    }

    private static int CalculateRiskScore(List<HoneypotTrack.Domain.Entities.AuditLog> logs)
    {
        var score = 0;

        // Base score por cantidad de intentos
        score += Math.Min(logs.Count * 5, 30);

        // Score adicional por tipos de amenazas cr�ticas
        var criticalThreats = logs.Count(a => 
            a.EntityName == "SQL_INJECTION" || 
            a.EntityName == "COMMAND_INJECTION");
        score += criticalThreats * 10;

        // Score por honeypots activados (indica reconocimiento activo)
        var honeypotTriggers = logs.Count(a => a.ActionType!.StartsWith("HONEYPOT_"));
        score += honeypotTriggers * 3;

        // Score por persistencia (actividad en m�ltiples d�as)
        var activeDays = logs.Select(a => a.Timestamp.Date).Distinct().Count();
        score += activeDays * 5;

        return Math.Min(score, 100); // M�ximo 100
    }

    #endregion
}

#region Response DTOs

public class SecurityStatsResponse
{
    public string? Period { get; set; }
    public int TotalThreats { get; set; }
    public int TotalHoneypotTriggers { get; set; }
    public int UniqueAttackerIPs { get; set; }
    public Dictionary<string, int>? ThreatsByType { get; set; }
    public Dictionary<string, int>? HoneypotTriggersByType { get; set; }
    public Dictionary<string, int>? ThreatsByDay { get; set; }
    public List<AttackerIpSummary>? TopAttackerIPs { get; set; }
    public List<BlockedIpSummary>? BlockedIPs { get; set; }
}

public class AttackerIpSummary
{
    public string? IpAddress { get; set; }
    public int TotalAttempts { get; set; }
    public List<string?>? ThreatTypes { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}

public class BlockedIpSummary
{
    public string? IpAddress { get; set; }
    public DateTime BlockedUntil { get; set; }
    public string? Reason { get; set; }
    public int ThreatCount { get; set; }
    public int RemainingMinutes { get; set; }
}

public class AttackerProfileResponse
{
    public string? IpAddress { get; set; }
    public int TotalAttempts { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsCurrentlyBlocked { get; set; }
    public List<string?>? UserAgents { get; set; }
    public List<string?>? Browsers { get; set; }
    public List<string?>? OperatingSystems { get; set; }
    public Dictionary<string, int>? ThreatTypes { get; set; }
    public Dictionary<string, int>? TargetedEndpoints { get; set; }
    public List<AttackTimelineEntry>? Timeline { get; set; }
    public int RiskScore { get; set; }
}

public class AttackTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string? ActionType { get; set; }
    public string? ThreatType { get; set; }
    public string? Endpoint { get; set; }
    public string? Description { get; set; }
    public int Severity { get; set; }
}

public class AttackListResponse
{
    public List<AttackEntry>? Attacks { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AttackEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? ActionType { get; set; }
    public string? ThreatType { get; set; }
    public string? Endpoint { get; set; }
    public string? Method { get; set; }
    public string? Description { get; set; }
    public string? UserAgent { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
}

public class RealtimeMetrics
{
    public int ThreatsLastHour { get; set; }
    public int ThreatsLast24Hours { get; set; }
    public int CurrentlyBlockedIPs { get; set; }
    public DateTime? LastThreatTimestamp { get; set; }
    public string? LastThreatType { get; set; }
    public string? LastThreatIP { get; set; }
    public string? SystemStatus { get; set; }
}

#endregion
