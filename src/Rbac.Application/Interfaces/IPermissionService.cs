namespace Rbac.Application.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken);
}
