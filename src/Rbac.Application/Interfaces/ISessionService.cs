using Rbac.Domain.Entities;

namespace Rbac.Application.Interfaces;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken);
    Task RevokeSessionAsync(Guid sessionId, string revokedBy, CancellationToken cancellationToken);
    Task RevokeAllSessionsAsync(Guid userId, string revokedBy, CancellationToken cancellationToken);
    Task<IEnumerable<Session>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<DeviceFingerprintRecord>> GetTrustedDevicesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<Session>> GetSuspiciousSessionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<SecurityEvent>> GetActiveThreatsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IEnumerable<DeviceFingerprintRecord>> GetDeviceHistoryAsync(Guid userId, CancellationToken cancellationToken);
    Task MarkSessionSuspiciousAsync(Guid sessionId, string reason, CancellationToken cancellationToken);
    Task MarkRefreshTokenSessionSuspiciousAsync(Guid refreshTokenId, string reason, CancellationToken cancellationToken);
    Task CompromiseSessionsAsync(Guid userId, IEnumerable<Guid> refreshTokenIds, string reason, CancellationToken cancellationToken);
    Task UpdateLastSeenAsync(Guid sessionId, CancellationToken cancellationToken);
}
