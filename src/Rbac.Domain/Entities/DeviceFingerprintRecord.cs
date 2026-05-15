using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public sealed class DeviceFingerprintRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string Fingerprint { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string AcceptLanguage { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string HeaderSignature { get; set; } = string.Empty;

    public bool IsTrusted { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime? SuspiciousChangeDetectedAtUtc { get; set; }
    public int Occurrences { get; set; }
    public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
}
