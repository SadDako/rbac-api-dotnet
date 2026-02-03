using System.Security.Claims;
using Rbac.Api.Application.Activity;

namespace Rbac.Api.Application.Audit;

public sealed class AuditService : IAuditService
{
    private readonly IActivityStore _activityStore;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IActivityStore activityStore, ILogger<AuditService> logger)
    {
        _activityStore = activityStore;
        _logger = logger;
    }

    public async Task RecordAsync(
        ClaimsPrincipal actor,
        string action,
        string target,
        string? details,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var actorName =
            actor.FindFirstValue(ClaimTypes.Email) ??
            actor.FindFirstValue(ClaimTypes.NameIdentifier) ??
            actor.Identity?.Name ??
            "unknown";

        var atUtc = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "AUDIT action={Action} target={Target} actor={Actor} atUtc={AtUtc} correlationId={CorrelationId} details={Details}",
            action,
            target,
            actorName,
            atUtc,
            correlationId,
            details ?? string.Empty);

        await _activityStore.AddAsync(
            new ActivityEvent
            {
                Type = "audit",
                Status = "success",
                Label = action,
                Description = string.IsNullOrWhiteSpace(details) ? target : $"{target} | {details}",
                AtUtc = atUtc,
                Source = "backend",
                Actor = actorName,
                CorrelationId = correlationId
            },
            cancellationToken);
    }
}
