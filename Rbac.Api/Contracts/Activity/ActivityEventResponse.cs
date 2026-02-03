namespace Rbac.Api.Contracts.Activity;

public sealed record ActivityEventResponse(
    string Id,
    string Type,
    string Status,
    string Label,
    string? Description,
    DateTimeOffset AtUtc,
    string Source,
    string? Actor,
    string CorrelationId);
