using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private const string AdminRoleName = "Admin";
    private readonly AppDbContext _db;

    public AdminUsersController(AppDbContext db)
    {
        _db = db;
    }

    // POST /admin/users/{userId}/promote-admin
    [HttpPost("{userId:guid}/promote-admin")]
    public async Task<IActionResult> PromoteToAdmin(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return NotFound(new { message = "Usuário não encontrado." });

        var adminRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == AdminRoleName, ct);

        if (adminRole is null)
            return NotFound(new { message = "Role Admin não existe. Rode o seed." });

        var alreadyAdmin = user.UserRoles.Any(ur => ur.Role.Name == AdminRoleName);
        if (alreadyAdmin)
            return Ok(new { message = "Usuário já é Admin.", userId });

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Usuário promovido para Admin.", userId });
    }

    // POST /admin/users/{userId}/demote-admin
    [HttpPost("{userId:guid}/demote-admin")]
    public async Task<IActionResult> DemoteFromAdmin(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return NotFound(new { message = "Usuário não encontrado." });

        var adminLink = user.UserRoles
            .FirstOrDefault(ur => ur.Role.Name == AdminRoleName);

        if (adminLink is null)
            return Ok(new { message = "Usuário não é Admin.", userId });

        _db.UserRoles.Remove(adminLink);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Admin removido do usuário.", userId });
    }
}
