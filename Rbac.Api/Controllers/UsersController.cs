using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Application.Interfaces;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserStore _store;

    public UsersController(IUserStore store)
    {
        _store = store;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
            return Unauthorized(new { message = "Token inválido (sub ausente)." });

        var user = await _store.FindByIdAsync(userId);
        if (user is null)
            return NotFound(new { message = "Usuário não encontrado." });

        var roles = await _store.GetUserRolesAsync(user.Id);

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            Roles = roles
        });
    }
}
