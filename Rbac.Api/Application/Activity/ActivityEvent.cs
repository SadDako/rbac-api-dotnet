namespace Rbac.Api.Application.Activity;

public sealed class ActivityEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset AtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "backend";
    public string? Actor { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
