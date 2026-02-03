using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Application.Authorization;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("test")]
public class TestAuthController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            name = User.FindFirstValue("name"),
            roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray()
        });
    }

    [HttpGet("admin")]
    [Authorize]
    [RequirePermission("admin.access")]
    public IActionResult AdminOnly()
    {
        return Ok(new { ok = true, message = "Admin access granted." });
    }
}
