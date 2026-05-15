using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Contracts.Users;
using Rbac.Api.Infrastructure.Data;

namespace Rbac.Api.Controllers;

[ApiController]
[Authorize(Policy = "UserPolicy")]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Perfil do usuário logado
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        // tenta sub, depois NameIdentifier (quando o runtime mapeia sub para NameIdentifier)
        var userIdValue =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Token inválido." });
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        var response = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray()
        };

        return Ok(response);
    }

    // Lista usuários (somente Admin)
    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Name
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}
