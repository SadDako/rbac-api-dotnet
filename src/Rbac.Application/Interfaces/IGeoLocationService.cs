using Rbac.Application.Security;

namespace Rbac.Application.Interfaces;

public interface IGeoLocationService
{
    Task<GeoLocationResult> LocateAsync(string ipAddress, CancellationToken cancellationToken);
}
