using Microsoft.AspNetCore.Mvc;
using HoneypotTrack.Domain.Entities;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Controllers.Honeypot;

/// <summary>
/// ?? HONEYPOT - Endpoints que simulan archivos sensibles
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class SensitiveFilesController(AppDbContext dbContext, ILogger<SensitiveFilesController> logger) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<SensitiveFilesController> _logger = logger;

    /// <summary>
    /// ?? Señuelo: .env file
    /// </summary>
    [HttpGet(".env")]
    [HttpGet("api/.env")]
    [HttpGet("app/.env")]
    public async Task<IActionResult> FakeEnvFile()
    {
        await LogHoneypotActivity("ENV_FILE_ACCESS");

        var fakeEnv = @"
# Database Configuration
DB_HOST=db.internal.local
DB_PORT=5432
DB_NAME=production
DB_USER=app_user
DB_PASSWORD=SuperSecret123!

# API Keys
STRIPE_SECRET_KEY=sk_live_FAKE_" + Guid.NewGuid().ToString("N") + @"
AWS_ACCESS_KEY_ID=AKIAFAKE" + Guid.NewGuid().ToString("N")[..16].ToUpper() + @"
AWS_SECRET_ACCESS_KEY=fake_secret_" + Guid.NewGuid().ToString("N") + @"

# JWT
JWT_SECRET=super_secret_jwt_key_dont_share
JWT_EXPIRATION=3600

# Email
SMTP_HOST=smtp.company.com
SMTP_USER=noreply@company.com
SMTP_PASS=email_password_123
";

        return Content(fakeEnv, "text/plain");
    }

    /// <summary>
    /// ?? Señuelo: config.php
    /// </summary>
    [HttpGet("config.php")]
    [HttpGet("wp-config.php")]
    [HttpGet("configuration.php")]
    public async Task<IActionResult> FakePhpConfig()
    {
        await LogHoneypotActivity("PHP_CONFIG_ACCESS");

        var fakeConfig = @"<?php
// Database settings
define('DB_NAME', 'wordpress_db');
define('DB_USER', 'wp_admin');
define('DB_PASSWORD', 'WpAdmin2024!');
define('DB_HOST', 'localhost');

// Security Keys (fake)
define('AUTH_KEY', '" + Guid.NewGuid().ToString() + @"');
define('SECURE_AUTH_KEY', '" + Guid.NewGuid().ToString() + @"');
define('LOGGED_IN_KEY', '" + Guid.NewGuid().ToString() + @"');

// Debug
define('WP_DEBUG', true);
?>";

        return Content(fakeConfig, "text/plain");
    }

    /// <summary>
    /// ?? Señuelo: robots.txt con rutas "ocultas"
    /// </summary>
    [HttpGet("robots.txt")]
    public async Task<IActionResult> FakeRobotsTxt()
    {
        await LogHoneypotActivity("ROBOTS_TXT_ACCESS");

        var robots = @"User-agent: *
Allow: /

# Sensitive directories - DO NOT INDEX
Disallow: /admin/
Disallow: /api/admin/
Disallow: /backup/
Disallow: /config/
Disallow: /database/
Disallow: /internal/
Disallow: /private/
Disallow: /secret/
Disallow: /.git/
Disallow: /.svn/
";

        return Content(robots, "text/plain");
    }

    /// <summary>
    /// ?? Señuelo: .git/config
    /// </summary>
    [HttpGet(".git/config")]
    [HttpGet(".git/HEAD")]
    public async Task<IActionResult> FakeGitConfig()
    {
        await LogHoneypotActivity("GIT_CONFIG_ACCESS");

        var gitConfig = @"[core]
    repositoryformatversion = 0
    filemode = true
    bare = false
    logallrefupdates = true
[remote ""origin""]
    url = https://github.com/company/internal-api.git
    fetch = +refs/heads/*:refs/remotes/origin/*
[branch ""main""]
    remote = origin
    merge = refs/heads/main
[user]
    name = Developer
    email = dev@company-internal.com
";

        return Content(gitConfig, "text/plain");
    }

    /// <summary>
    /// ?? Señuelo: phpinfo
    /// </summary>
    [HttpGet("phpinfo.php")]
    [HttpGet("info.php")]
    [HttpGet("test.php")]
    public async Task<IActionResult> FakePhpInfo()
    {
        await LogHoneypotActivity("PHPINFO_ACCESS");

        return Ok(new
        {
            php_version = "8.2.0",
            server_api = "apache2handler",
            document_root = "/var/www/html",
            server_admin = "admin@company.com",
            mysql_version = "8.0.32",
            loaded_extensions = new[] { "mysqli", "pdo", "curl", "gd", "xml" }
        });
    }

    /// <summary>
    /// ?? Señuelo: Swagger/API docs alternativos
    /// </summary>
    [HttpGet("api-docs")]
    [HttpGet("swagger.json")]
    [HttpGet("openapi.json")]
    public async Task<IActionResult> FakeApiDocs()
    {
        await LogHoneypotActivity("API_DOCS_ACCESS");

        return Ok(new
        {
            openapi = "3.0.0",
            info = new { title = "Internal API", version = "2.0.0" },
            paths = new
            {
                admin_login = new { post = new { summary = "Admin authentication" } },
                admin_users = new { get = new { summary = "List all users" } },
                admin_config = new { get = new { summary = "Get system configuration" } },
                admin_backup = new { post = new { summary = "Create database backup" } }
            }
        });
    }

    /// <summary>
    /// ?? Señuelo: Actuator/Health endpoints (Spring Boot style)
    /// </summary>
    [HttpGet("actuator")]
    [HttpGet("actuator/env")]
    [HttpGet("actuator/heapdump")]
    public async Task<IActionResult> FakeActuator()
    {
        await LogHoneypotActivity("ACTUATOR_ACCESS");

        return Ok(new
        {
            status = "UP",
            environment = new
            {
                database_url = "jdbc:mysql://localhost:3306/app",
                database_password = "***",
                api_secret = "hidden"
            },
            system = new
            {
                java_version = "17.0.1",
                heap_used = "256MB",
                heap_max = "1024MB"
            }
        });
    }

    private async Task LogHoneypotActivity(string activityType)
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
            IsSuccessful = false,
            ActionType = "HONEYPOT_" + activityType,
            EntityName = "Honeypot",
            ErrorMessage = $"Sensitive file access attempt: {activityType}",
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
            "?? HONEYPOT: Sensitive file access - Type: {ActivityType}, IP: {Ip}",
            activityType, clientIp);
    }
}
