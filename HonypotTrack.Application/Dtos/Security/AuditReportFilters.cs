using HonypotTrack.Application.Commons.Bases;

namespace HonypotTrack.Application.Dtos.Security;

public class AuditReportFilters : BaseFilters
{
    public string? IpAddress { get; set; }
    public string? UserEmail { get; set; }
    public string? ActionType { get; set; }
    public string? ThreatType { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public bool? IsSuccessful { get; set; }
    public bool OnlyThreats { get; set; }
    public bool OnlyHoneypot { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
}
