using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public sealed class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public string CreatedByIp { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? ParentTokenHash { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public string? DeviceFingerprint { get; set; }
    public Guid TokenFamilyId { get; set; } = Guid.NewGuid();
    public bool IsCompromised { get; set; }
    public DateTime? CompromisedAtUtc { get; set; }
    public DateTime? ReuseDetectedAtUtc { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => !IsRevoked && !IsExpired && !IsCompromised;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
