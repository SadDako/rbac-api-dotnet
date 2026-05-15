using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;
using Rbac.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Rbac.Infrastructure.Repositories;

public sealed class SecurityEventRepository : ISecurityEventRepository
{
    private readonly AppDbContext _dbContext;

    public SecurityEventRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        await _dbContext.Set<SecurityEvent>().AddAsync(securityEvent, cancellationToken);
    }

    public async Task<IEnumerable<SecurityEvent>> ListRecentByUserAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<SecurityEvent>()
            .Where(e => e.UserId == userId && e.CreatedAtUtc >= sinceUtc)
            .OrderByDescending(e => e.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
