using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public sealed class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }
    public bool LockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("D");

    public bool SoftDeleted { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<DeviceFingerprintRecord> DeviceFingerprints { get; set; } = new List<DeviceFingerprintRecord>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // MFA
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    // Recovery codes are stored as a JSON array of hashed codes
    public string? RecoveryCodes { get; set; }
}
