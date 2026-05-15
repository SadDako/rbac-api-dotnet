using System.ComponentModel.DataAnnotations;

namespace Rbac.Domain.Entities;

public sealed class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string HttpMethod { get; set; } = string.Empty;

    public int StatusCode { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
