using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize]
[RequirePermission("admin.access")]
public class AdminController : ControllerBase
{
    // GET /admin/ping
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            atUtc = DateTime.UtcNow,
            traceId = HttpContext.TraceIdentifier,
            correlationId = HttpContext.GetCorrelationId()
        });
    }

    // GET /admin/whoami
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            traceId = HttpContext.TraceIdentifier,
            correlationId = HttpContext.GetCorrelationId(),
            claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            })
        });
    }
}
