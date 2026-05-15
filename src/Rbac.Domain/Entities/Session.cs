using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public sealed class Session
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? RefreshTokenId { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsRevoked { get; set; }
    public bool IsSuspicious { get; set; }
    public bool IsCompromised { get; set; }
    public bool RequiresMfa { get; set; }
    public int RiskScore { get; set; }
    public Guid? TokenFamilyId { get; set; }

    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? CompromisedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
}
