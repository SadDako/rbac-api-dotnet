using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface IDeviceFingerprintRepository
{
    Task<DeviceFingerprintRecord?> GetByUserAndFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken);
    Task AddAsync(DeviceFingerprintRecord record, CancellationToken cancellationToken);
    Task<IEnumerable<DeviceFingerprintRecord>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
