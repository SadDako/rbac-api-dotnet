using System.Collections.Concurrent;

namespace Rbac.Api.Application.Activity;

public sealed class InMemoryActivityStore : IActivityStore
{
    private const int MaxEvents = 500;
    private readonly ConcurrentQueue<ActivityEvent> _events = new();

    public Task<ActivityEvent> AddAsync(ActivityEvent activityEvent, CancellationToken cancellationToken = default)
    {
        _events.Enqueue(activityEvent);

        while (_events.Count > MaxEvents && _events.TryDequeue(out _))
        {
        }

        return Task.FromResult(activityEvent);
    }

    public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var snapshot = _events.ToArray();
        Array.Reverse(snapshot);

        return Task.FromResult<IReadOnlyList<ActivityEvent>>(snapshot.Take(safeLimit).ToList());
    }
}
