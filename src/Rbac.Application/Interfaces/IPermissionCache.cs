namespace Rbac.Application.Interfaces;

public interface IPermissionCache
{
    Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);
}
