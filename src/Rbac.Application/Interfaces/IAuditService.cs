using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface IAuditService
{
    Task TrackAsync(AuditLog auditLog, CancellationToken cancellationToken);
}
