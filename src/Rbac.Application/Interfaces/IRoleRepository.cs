using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Role role, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
