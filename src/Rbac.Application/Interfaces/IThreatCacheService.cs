using Rbac.Application.Security;

namespace Rbac.Application.Interfaces;

public interface IThreatCacheService
{
    Task<ThreatCounterResult> IncrementCounterAsync(string key, TimeSpan window, long limit, CancellationToken cancellationToken);
    Task ResetCounterAsync(string key, CancellationToken cancellationToken);
    Task SetFlagAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
    Task<bool> IsFlaggedAsync(string key, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
    Task<IAsyncDisposable?> TryAcquireLockAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
}
