using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Domain.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedByIp { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    [Required]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => RevokedAtUtc is null && !IsExpired;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
