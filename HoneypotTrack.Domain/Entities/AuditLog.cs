using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoneypotTrack.Domain.Entities;

[Table("AuditLogs", Schema = "empresa")]
public class AuditLog
{
    [Key]
    [Column("AuditLogId")]
    public int AuditLogId { get; set; }

    // Información de la petición
    [Column("HttpMethod")]
    [StringLength(10)]
    public string HttpMethod { get; set; } = null!;

    [Column("RequestUrl")]
    [StringLength(500)]
    public string RequestUrl { get; set; } = null!;

    [Column("RequestPath")]
    [StringLength(300)]
    public string RequestPath { get; set; } = null!;

    [Column("QueryString")]
    [StringLength(1000)]
    public string? QueryString { get; set; }

    [Column("RequestBody")]
    public string? RequestBody { get; set; }

    [Column("ResponseBody")]
    public string? ResponseBody { get; set; }

    [Column("StatusCode")]
    public int StatusCode { get; set; }

    // Información del cliente
    [Column("IpAddress")]
    [StringLength(50)]
    public string? IpAddress { get; set; }

    [Column("MacAddress")]
    [StringLength(50)]
    public string? MacAddress { get; set; }

    [Column("UserAgent")]
    [StringLength(500)]
    public string? UserAgent { get; set; }

    [Column("Browser")]
    [StringLength(100)]
    public string? Browser { get; set; }

    [Column("BrowserVersion")]
    [StringLength(50)]
    public string? BrowserVersion { get; set; }

    [Column("OperatingSystem")]
    [StringLength(100)]
    public string? OperatingSystem { get; set; }

    [Column("DeviceType")]
    [StringLength(50)]
    public string? DeviceType { get; set; }

    // Información del usuario
    [Column("UserId")]
    public int? UserId { get; set; }

    [Column("UserName")]
    [StringLength(100)]
    public string? UserName { get; set; }

    [Column("UserEmail")]
    [StringLength(100)]
    public string? UserEmail { get; set; }

    // Información de la acción
    [Column("ActionType")]
    [StringLength(50)]
    public string? ActionType { get; set; } // CREATE, READ, UPDATE, DELETE

    [Column("EntityName")]
    [StringLength(100)]
    public string? EntityName { get; set; }

    [Column("EntityId")]
    [StringLength(50)]
    public string? EntityId { get; set; }

    [Column("OldValues")]
    public string? OldValues { get; set; }

    [Column("NewValues")]
    public string? NewValues { get; set; }

    [Column("ChangedColumns")]
    [StringLength(500)]
    public string? ChangedColumns { get; set; }

    // Información de rendimiento
    [Column("ExecutionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [Column("RequestSize")]
    public long? RequestSize { get; set; }

    [Column("ResponseSize")]
    public long? ResponseSize { get; set; }

    // Información adicional
    [Column("ServerName")]
    [StringLength(100)]
    public string? ServerName { get; set; }

    [Column("Environment")]
    [StringLength(50)]
    public string? Environment { get; set; }

    [Column("CorrelationId")]
    [StringLength(50)]
    public string? CorrelationId { get; set; }

    [Column("SessionId")]
    [StringLength(100)]
    public string? SessionId { get; set; }

    [Column("Referer")]
    [StringLength(500)]
    public string? Referer { get; set; }

    [Column("Origin")]
    [StringLength(200)]
    public string? Origin { get; set; }

    [Column("IsSuccessful")]
    public bool IsSuccessful { get; set; }

    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [Column("ExceptionDetails")]
    public string? ExceptionDetails { get; set; }

    // Timestamps
    [Column("Timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("LocalTimestamp")]
    public DateTime LocalTimestamp { get; set; } = DateTime.Now;
}
