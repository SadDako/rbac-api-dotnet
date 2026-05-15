using Microsoft.EntityFrameworkCore;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;
using Rbac.Infrastructure.Data;

namespace Rbac.Infrastructure.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _dbContext;

    public SessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken)
    {
        await _dbContext.Sessions.AddAsync(session, cancellationToken);
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Session?> GetByRefreshTokenIdAsync(Guid refreshTokenId, CancellationToken cancellationToken)
    {
        return await _dbContext.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenId == refreshTokenId, cancellationToken);
    }

    public async Task<IEnumerable<Session>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Session>> ListByRefreshTokenIdsAsync(IEnumerable<Guid> refreshTokenIds, CancellationToken cancellationToken)
    {
        var ids = refreshTokenIds.ToArray();
        return await _dbContext.Sessions
            .Where(s => s.RefreshTokenId.HasValue && ids.Contains(s.RefreshTokenId.Value))
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Session session, CancellationToken cancellationToken)
    {
        _dbContext.Sessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
