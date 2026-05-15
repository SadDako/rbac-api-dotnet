using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken cancellationToken);
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Session?> GetByRefreshTokenIdAsync(Guid refreshTokenId, CancellationToken cancellationToken);
    Task<IEnumerable<Session>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<Session>> ListByRefreshTokenIdsAsync(IEnumerable<Guid> refreshTokenIds, CancellationToken cancellationToken);
    Task UpdateAsync(Session session, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
