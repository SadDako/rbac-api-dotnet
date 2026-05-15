using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public enum SecurityEventType
{
    SuspiciousLogin,
    ImpossibleTravel,
    BruteForce,
    CredentialStuffing,
    TokenReuse,
    NewDevice,
    FingerprintMismatch,
    DeviceAnomaly,
    MFABypassAttempt,
    ExcessiveRefresh,
    PermissionEscalation,
    SuspiciousHeaders,
    MalformedToken,
    CompromisedSession
}

public sealed class SecurityEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public SecurityEventType Type { get; set; }
    public int Severity { get; set; }
    public double RiskScore { get; set; }

    public string IPAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
