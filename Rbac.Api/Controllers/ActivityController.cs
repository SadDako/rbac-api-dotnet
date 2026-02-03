using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Application.Activity;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Contracts.Activity;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("activity")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityStore _activityStore;

    public ActivityController(IActivityStore activityStore)
    {
        _activityStore = activityStore;
    }

    [HttpGet]
    [RequirePermission("activity.read")]
    public async Task<ActionResult<IReadOnlyList<ActivityEventResponse>>> List([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var events = await _activityStore.GetRecentAsync(limit, cancellationToken);

        var response = events
            .Select(item => new ActivityEventResponse(
                item.Id,
                item.Type,
                item.Status,
                item.Label,
                item.Description,
                item.AtUtc,
                item.Source,
                item.Actor,
                item.CorrelationId))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("activity.write")]
    public async Task<IActionResult> Create(ActivityEventRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Type)
            || string.IsNullOrWhiteSpace(request.Status)
            || string.IsNullOrWhiteSpace(request.Label))
        {
            return this.ToApiProblem(StatusCodes.Status400BadRequest, "activity.invalid_payload", "Type, status and label are required.");
        }

        var actor =
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.Identity?.Name;

        var created = await _activityStore.AddAsync(
            new ActivityEvent
            {
                Type = request.Type.Trim().ToLowerInvariant(),
                Status = request.Status.Trim().ToLowerInvariant(),
                Label = request.Label.Trim(),
                Description = request.Description?.Trim(),
                AtUtc = DateTimeOffset.UtcNow,
                Source = "client",
                Actor = actor,
                CorrelationId = HttpContext.GetCorrelationId()
            },
            cancellationToken);

        var response = new ActivityEventResponse(
            created.Id,
            created.Type,
            created.Status,
            created.Label,
            created.Description,
            created.AtUtc,
            created.Source,
            created.Actor,
            created.CorrelationId);

        return Created($"/activity/{created.Id}", response);
    }
}
