using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface ISecurityEventService
{
    Task<RecordSecurityEventResult> RecordEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
}

public sealed class RecordSecurityEventResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
