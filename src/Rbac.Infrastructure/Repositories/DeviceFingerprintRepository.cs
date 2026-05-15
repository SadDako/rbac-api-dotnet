using Microsoft.EntityFrameworkCore;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;
using Rbac.Infrastructure.Data;

namespace Rbac.Infrastructure.Repositories;

public sealed class DeviceFingerprintRepository : IDeviceFingerprintRepository
{
    private readonly AppDbContext _dbContext;

    public DeviceFingerprintRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DeviceFingerprintRecord record, CancellationToken cancellationToken)
    {
        await _dbContext.Set<DeviceFingerprintRecord>().AddAsync(record, cancellationToken);
    }

    public async Task<DeviceFingerprintRecord?> GetByUserAndFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DeviceFingerprintRecord>()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Fingerprint == fingerprint, cancellationToken);
    }

    public async Task<IEnumerable<DeviceFingerprintRecord>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DeviceFingerprintRecord>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.LastSeenAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
