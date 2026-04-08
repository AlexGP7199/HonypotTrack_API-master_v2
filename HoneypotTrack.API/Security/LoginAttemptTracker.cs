using System.Collections.Concurrent;

namespace HoneypotTrack.API.Security;

public interface ILoginAttemptTracker
{
    FailedLoginDecision RegisterFailure(string? ipAddress, string? email);
    void Reset(string? ipAddress, string? email);
}

public record FailedLoginDecision(int AttemptCount, bool ShouldActivateHoneypot);

public class LoginAttemptTracker(IConfiguration configuration) : ILoginAttemptTracker
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<string, LoginAttemptState> _attempts = new();

    public FailedLoginDecision RegisterFailure(string? ipAddress, string? email)
    {
        var key = BuildKey(ipAddress, email);
        var windowMinutes = _configuration.GetValue<int?>("Security:Honeypot:FailedLoginWindowMinutes") ?? 10;
        var threshold = _configuration.GetValue<int?>("Security:Honeypot:FailedLoginThreshold") ?? 3;
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-windowMinutes);

        var state = _attempts.AddOrUpdate(
            key,
            _ => new LoginAttemptState { Count = 1, LastAttemptUtc = now },
            (_, existing) =>
            {
                if (existing.LastAttemptUtc < windowStart)
                {
                    existing.Count = 1;
                }
                else
                {
                    existing.Count++;
                }

                existing.LastAttemptUtc = now;
                return existing;
            });

        return new FailedLoginDecision(state.Count, state.Count >= threshold);
    }

    public void Reset(string? ipAddress, string? email)
    {
        _attempts.TryRemove(BuildKey(ipAddress, email), out _);
    }

    private static string BuildKey(string? ipAddress, string? email)
    {
        var normalizedIp = string.IsNullOrWhiteSpace(ipAddress) ? "unknown-ip" : ipAddress.Trim().ToLowerInvariant();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? "unknown-email" : email.Trim().ToLowerInvariant();
        return $"{normalizedIp}|{normalizedEmail}";
    }

    private sealed class LoginAttemptState
    {
        public int Count { get; set; }
        public DateTime LastAttemptUtc { get; set; }
    }
}
