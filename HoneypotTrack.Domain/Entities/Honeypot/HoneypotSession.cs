namespace HoneypotTrack.Domain.Entities;

/// <summary>
/// Sesión de honeypot para trackear atacantes
/// </summary>
public class HoneypotSession
{
    public int SessionId { get; set; }
    public string SessionToken { get; set; } = null!;
    public string AttackerIp { get; set; } = null!;
    public string? InitialThreatType { get; set; }
    public string? InitialPayload { get; set; }
    public string? UserAgent { get; set; }
    public int? AssignedUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivity { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalRequests { get; set; } = 0;
    public int TotalThreatsDetected { get; set; } = 0;
}
