namespace Rbac.Api.Application.Activity;

public interface IActivityStore
{
    Task<ActivityEvent> AddAsync(ActivityEvent activityEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default);
}
