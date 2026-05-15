using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface ISecurityEventRepository
{
    Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
    Task<IEnumerable<SecurityEvent>> ListRecentByUserAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
