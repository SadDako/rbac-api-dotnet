using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;

    public AuditService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task TrackAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _repository.AddAsync(auditLog, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
