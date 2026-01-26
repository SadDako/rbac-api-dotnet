using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rbac.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }
}
