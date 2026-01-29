using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
            sub = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            name = User.FindFirstValue("name"),
            roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray()
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { ok = true, message = "Você é Admin ✅" });
    }
}
