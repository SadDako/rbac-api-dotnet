using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : ControllerBase
{
    // GET /admin/ping
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            atUtc = DateTime.UtcNow
        });
    }

    // GET /admin/whoami
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            claims = User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            })
        });
    }
}
