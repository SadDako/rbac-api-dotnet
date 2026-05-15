using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    private readonly ISessionRepository _repository;
    private readonly IDeviceFingerprintRepository _deviceFingerprintRepository;
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly IDistributedCache? _cache;
    private readonly IAuditService _auditService;
    private readonly ISecurityMetrics _metrics;

    private const string UserSessionsCacheKey = "user:sessions:{0}";

    public SessionService(
        ISessionRepository repository,
        IDeviceFingerprintRepository deviceFingerprintRepository,
        ISecurityEventRepository securityEventRepository,
        IDistributedCache? cache,
        IAuditService auditService,
        ISecurityMetrics metrics)
    {
        _repository = repository;
        _deviceFingerprintRepository = deviceFingerprintRepository;
        _securityEventRepository = securityEventRepository;
        _cache = cache;
        _auditService = auditService;
        _metrics = metrics;
    }

    public async Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken)
    {
        session.CreatedAtUtc = DateTime.UtcNow;
        session.LastSeenAtUtc = DateTime.UtcNow;
        await _repository.AddAsync(session, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(session.UserId);

        // audit
        await _auditService.TrackAsync(new AuditLog
        {
            UserId = session.UserId,
            Action = "session.create",
            Endpoint = "/session",
            HttpMethod = "POST",
            IpAddress = session.IPAddress,
            UserAgent = session.UserAgent,
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { session.DeviceName, session.Browser, session.OS })
        }, cancellationToken);

        return session;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string revokedBy, CancellationToken cancellationToken)
    {
        var s = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (s is null) return;
        s.IsActive = false;
        s.IsRevoked = true;
        s.RevokedAtUtc = DateTime.UtcNow;
        s.RevokedReason = revokedBy;
        await _repository.UpdateAsync(s, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(s.UserId);

        await _auditService.TrackAsync(new AuditLog
        {
            UserId = s.UserId,
            Action = "session.revoke",
            Endpoint = "/session/revoke",
            HttpMethod = "DELETE",
            IpAddress = s.IPAddress,
            UserAgent = s.UserAgent,
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { s.Id })
        }, cancellationToken);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string revokedBy, CancellationToken cancellationToken)
    {
        var sessions = await _repository.ListByUserAsync(userId, cancellationToken);
        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.IsRevoked = true;
            s.RevokedAtUtc = DateTime.UtcNow;
            s.RevokedReason = revokedBy;
            await _repository.UpdateAsync(s, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(userId);

        await _auditService.TrackAsync(new AuditLog
        {
            UserId = userId,
            Action = "session.revoke_all",
            Endpoint = "/sessions/revoke-all",
            HttpMethod = "DELETE",
            IpAddress = string.Empty,
            UserAgent = string.Empty,
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { revokedBy })
        }, cancellationToken);
    }

    public async Task<IEnumerable<Session>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (_cache != null)
        {
            var key = string.Format(UserSessionsCacheKey, userId);
            var data = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(data))
            {
                return JsonSerializer.Deserialize<IEnumerable<Session>>(data) ?? Array.Empty<Session>();
            }

            var fromDb = await _repository.ListByUserAsync(userId, cancellationToken);
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(fromDb), new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) }, cancellationToken);
            return fromDb;
        }

        return await _repository.ListByUserAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<DeviceFingerprintRecord>> GetTrustedDevicesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var devices = await _deviceFingerprintRepository.ListByUserAsync(userId, cancellationToken);
        return devices.Where(d => d.IsTrusted);
    }

    public async Task<IEnumerable<Session>> GetSuspiciousSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await GetUserSessionsAsync(userId, cancellationToken);
        return sessions.Where(s => s.IsSuspicious || s.IsCompromised || s.RiskScore >= 31);
    }

    public async Task<IEnumerable<SecurityEvent>> GetActiveThreatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _securityEventRepository.ListRecentByUserAsync(userId, DateTime.UtcNow.AddDays(-7), cancellationToken);
    }

    public async Task<IEnumerable<DeviceFingerprintRecord>> GetDeviceHistoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _deviceFingerprintRepository.ListByUserAsync(userId, cancellationToken);
    }

    public async Task MarkSessionSuspiciousAsync(Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        var s = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (s is null) return;
        s.IsSuspicious = true;
        await _repository.UpdateAsync(s, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(s.UserId);

        await _auditService.TrackAsync(new AuditLog
        {
            UserId = s.UserId,
            Action = "session.suspicious",
            Endpoint = "/session/mark-suspicious",
            HttpMethod = "POST",
            IpAddress = s.IPAddress,
            UserAgent = s.UserAgent,
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { s.Id, reason })
        }, cancellationToken);
    }

    public async Task MarkRefreshTokenSessionSuspiciousAsync(Guid refreshTokenId, string reason, CancellationToken cancellationToken)
    {
        var s = await _repository.GetByRefreshTokenIdAsync(refreshTokenId, cancellationToken);
        if (s is null) return;

        s.IsSuspicious = true;
        await _repository.UpdateAsync(s, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(s.UserId);

        await _auditService.TrackAsync(new AuditLog
        {
            UserId = s.UserId,
            Action = "session.suspicious",
            Endpoint = "/session/refresh-threat",
            HttpMethod = "POST",
            IpAddress = s.IPAddress,
            UserAgent = s.UserAgent,
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { s.Id, refreshTokenId, reason })
        }, cancellationToken);
    }

    public async Task CompromiseSessionsAsync(Guid userId, IEnumerable<Guid> refreshTokenIds, string reason, CancellationToken cancellationToken)
    {
        var sessions = (await _repository.ListByRefreshTokenIdsAsync(refreshTokenIds, cancellationToken)).ToList();
        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.IsRevoked = true;
            s.IsSuspicious = true;
            s.IsCompromised = true;
            s.CompromisedAtUtc = DateTime.UtcNow;
            s.RevokedAtUtc = DateTime.UtcNow;
            s.RevokedReason = reason;
            await _repository.UpdateAsync(s, cancellationToken);
            _metrics.CompromisedSession();
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(userId);

        await _auditService.TrackAsync(new AuditLog
        {
            UserId = userId,
            Action = "session.compromise",
            Endpoint = "/sessions/compromise",
            HttpMethod = "POST",
            CorrelationId = string.Empty,
            Payload = JsonSerializer.Serialize(new { reason, sessionIds = sessions.Select(s => s.Id) })
        }, cancellationToken);
    }

    public async Task UpdateLastSeenAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var s = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (s is null) return;
        s.LastSeenAtUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(s, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync(s.UserId);
    }

    private async Task InvalidateCacheAsync(Guid userId)
    {
        if (_cache == null) return;
        var key = string.Format(UserSessionsCacheKey, userId);
        await _cache.RemoveAsync(key);
    }
}
