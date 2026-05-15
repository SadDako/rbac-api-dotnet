namespace Rbac.Application.Security;

public sealed class SecurityRequestContext
{
    public string ClientFingerprint { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public string AcceptLanguage { get; init; } = string.Empty;
    public string IPAddress { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string Browser { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> RelevantHeaders { get; init; } = new Dictionary<string, string>();
}

public sealed class GeoLocationResult
{
    public string Country { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public enum RiskLevel
{
    Low,
    Suspicious,
    Critical
}

public sealed class RiskAssessment
{
    public int Score { get; init; }
    public RiskLevel Level { get; init; }
    public bool RequiresMfa { get; init; }
    public bool ShouldThrottle { get; init; }
    public bool ShouldRevoke { get; init; }
    public IReadOnlyCollection<string> Signals { get; init; } = Array.Empty<string>();
}

public sealed class RiskEvaluationContext
{
    public Guid? UserId { get; init; }
    public SecurityRequestContext Request { get; init; } = new();
    public GeoLocationResult Location { get; init; } = new();
    public bool IsNewDevice { get; init; }
    public bool IsSuspiciousDevice { get; init; }
    public bool FingerprintMismatch { get; init; }
    public bool BruteForceDetected { get; init; }
    public bool MfaFailure { get; init; }
    public bool TokenReuseDetected { get; init; }
    public bool SuspiciousIp { get; init; }
    public bool ImpossibleTravel { get; init; }
    public bool RefreshAbuse { get; init; }
}

public sealed class BruteForceAssessment
{
    public bool IsBlocked { get; init; }
    public bool IsCredentialStuffing { get; init; }
    public bool IsDistributedAttack { get; init; }
    public TimeSpan RetryAfter { get; init; }
    public int RiskScore { get; init; }
}

public sealed class ThreatCounterResult
{
    public long Count { get; init; }
    public bool IsLimitExceeded { get; init; }
    public TimeSpan Ttl { get; init; }
}
