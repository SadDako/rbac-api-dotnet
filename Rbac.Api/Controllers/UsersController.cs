using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Application.Audit;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Contracts.Users;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public UsersController(AppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet("debug-claims")]
    public IActionResult DebugClaims()
    {
        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            authType = User.Identity?.AuthenticationType,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpGet("me")]
    [RequirePermission("users.me.read")]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return this.ToApiProblem(StatusCodes.Status401Unauthorized, "auth.invalid_token", "Token is invalid.");
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.not_found", "User was not found.");
        }

        var response = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray(),
            Permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Key)
                .Distinct()
                .OrderBy(key => key)
                .ToArray()
        };

        return Ok(response);
    }

    [HttpGet]
    [RequirePermission("users.read")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Name,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToArray()
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("users.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.not_found", "User was not found.");
        }

        var response = new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray(),
            Permissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Key)
                .Distinct()
                .OrderBy(key => key)
                .ToArray()
        };

        return Ok(response);
    }

    [HttpPost("{id:guid}/roles")]
    [RequirePermission("users.roles.assign")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.not_found", "User was not found.");
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "roles.not_found", "Role was not found.");
        }

        if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            return Ok(new { message = "Role already assigned." });
        }

        user.UserRoles.Add(new Domain.Entities.UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            User,
            "user.role.assigned",
            $"user:{user.Email}",
            $"role={role.Name}",
            HttpContext.GetCorrelationId(),
            cancellationToken);

        return Ok(new { message = "Role assigned successfully." });
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [RequirePermission("users.roles.remove")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        var link = await _dbContext.UserRoles
            .Include(ur => ur.Role)
            .Include(ur => ur.User)
            .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == roleId, cancellationToken);

        if (link is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.role_link_not_found", "User/role link was not found.");
        }

        _dbContext.UserRoles.Remove(link);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            User,
            "user.role.removed",
            $"user:{link.User.Email}",
            $"role={link.Role.Name}",
            HttpContext.GetCorrelationId(),
            cancellationToken);

        return Ok(new { message = "Role removed successfully." });
    }
}
