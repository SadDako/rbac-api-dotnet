using Rbac.Domain.Entities;
using Rbac.Application.Security;

namespace Rbac.Application.Interfaces;

public interface IDeviceFingerprintService
{
    DeviceFingerprintRecord CreateDescriptor(SecurityRequestContext context, GeoLocationResult location);
    Task<(DeviceFingerprintRecord Record, bool IsNewDevice, bool IsSuspicious)> TrackDeviceAsync(Guid userId, DeviceFingerprintRecord descriptor, CancellationToken cancellationToken);
}
