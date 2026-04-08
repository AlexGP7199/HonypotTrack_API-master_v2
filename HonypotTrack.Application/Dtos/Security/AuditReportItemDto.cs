namespace HonypotTrack.Application.Dtos.Security;

public class AuditReportItemDto
{
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserEmail { get; set; }
    public string HttpMethod { get; set; } = null!;
    public string RequestPath { get; set; } = null!;
    public int StatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ActionType { get; set; }
    public string? ThreatType { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorMessage { get; set; }
}
