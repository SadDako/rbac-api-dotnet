using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Services;

public sealed class SecurityEventService : ISecurityEventService
{
    private readonly ISecurityEventRepository _repository;
    private readonly ISecurityMetrics _metrics;

    public SecurityEventService(ISecurityEventRepository repository, ISecurityMetrics metrics)
    {
        _repository = repository;
        _metrics = metrics;
    }

    public async Task<RecordSecurityEventResult> RecordEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        await _repository.AddAsync(securityEvent, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        TrackMetrics(securityEvent);
        return new RecordSecurityEventResult { Success = true, Message = "Recorded" };
    }

    private void TrackMetrics(SecurityEvent securityEvent)
    {
        switch (securityEvent.Type)
        {
            case SecurityEventType.SuspiciousLogin:
                _metrics.SuspiciousLogin();
                break;
            case SecurityEventType.BruteForce:
            case SecurityEventType.CredentialStuffing:
                _metrics.BruteForceAttempt();
                break;
            case SecurityEventType.TokenReuse:
                _metrics.TokenReuse();
                break;
            case SecurityEventType.ImpossibleTravel:
                _metrics.ImpossibleTravel();
                break;
            case SecurityEventType.NewDevice:
            case SecurityEventType.DeviceAnomaly:
            case SecurityEventType.FingerprintMismatch:
                _metrics.SuspiciousDevice();
                break;
            case SecurityEventType.CompromisedSession:
                _metrics.CompromisedSession();
                break;
        }
    }
}
