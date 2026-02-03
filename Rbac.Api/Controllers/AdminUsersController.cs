using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize]
[RequirePermission("admin.access")]
public class AdminUsersController : ControllerBase
{
    private const string AdminRoleName = "Admin";
    private readonly AppDbContext _db;

    public AdminUsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("{userId:guid}/promote-admin")]
    public async Task<IActionResult> PromoteToAdmin(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.not_found", "User was not found.");
        }

        var adminRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == AdminRoleName, ct);

        if (adminRole is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "roles.not_found", "Admin role was not found.");
        }

        var alreadyAdmin = user.UserRoles.Any(ur => ur.Role.Name == AdminRoleName);
        if (alreadyAdmin)
        {
            return Ok(new { message = "User is already admin.", userId });
        }

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "User promoted to admin.", userId });
    }

    [HttpPost("{userId:guid}/demote-admin")]
    public async Task<IActionResult> DemoteFromAdmin(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "users.not_found", "User was not found.");
        }

        var adminLink = user.UserRoles
            .FirstOrDefault(ur => ur.Role.Name == AdminRoleName);

        if (adminLink is null)
        {
            return Ok(new { message = "User is not admin.", userId });
        }

        _db.UserRoles.Remove(adminLink);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Admin role removed from user.", userId });
    }
}
