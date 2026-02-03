using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Application.Audit;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Contracts.Roles;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public RolesController(AppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet]
    [RequirePermission("roles.read")]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> List(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse(
                r.Id,
                r.Name,
                r.RolePermissions.Select(rp => rp.Permission.Key).OrderBy(key => key).ToArray()))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpPost]
    [RequirePermission("roles.create")]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        if (await _dbContext.Roles.AnyAsync(r => r.Name == normalizedName, cancellationToken))
        {
            return this.ToApiProblem(StatusCodes.Status409Conflict, "roles.duplicate", "Role already exists.");
        }

        var role = new Role
        {
            Name = normalizedName
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            User,
            "role.created",
            $"role:{role.Name}",
            $"roleId={role.Id}",
            HttpContext.GetCorrelationId(),
            cancellationToken);

        return Created($"/roles/{role.Id}", new RoleResponse(role.Id, role.Name, Array.Empty<string>()));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("roles.update")]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "roles.not_found", "Role was not found.");
        }

        role.Name = request.Name.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Role updated." });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("roles.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "roles.not_found", "Role was not found.");
        }

        _dbContext.UserRoles.RemoveRange(role.UserRoles);
        _dbContext.RolePermissions.RemoveRange(role.RolePermissions);
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            User,
            "role.deleted",
            $"role:{role.Name}",
            $"roleId={role.Id}",
            HttpContext.GetCorrelationId(),
            cancellationToken);

        return Ok(new { message = "Role removed." });
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission("roles.permissions.update")]
    public async Task<IActionResult> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role is null)
        {
            return this.ToApiProblem(StatusCodes.Status404NotFound, "roles.not_found", "Role was not found.");
        }

        var requestedKeys = request.Permissions
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await _dbContext.Permissions
            .Where(p => requestedKeys.Contains(p.Key))
            .ToListAsync(cancellationToken);

        if (permissions.Count != requestedKeys.Length)
        {
            return this.ToApiProblem(StatusCodes.Status400BadRequest, "roles.invalid_permission", "One or more permissions are invalid.");
        }

        var toRemove = role.RolePermissions.Where(rp => !requestedKeys.Contains(rp.Permission.Key)).ToList();
        _dbContext.RolePermissions.RemoveRange(toRemove);

        foreach (var permission in permissions)
        {
            if (role.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
            {
                continue;
            }

            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            User,
            "role.permissions.updated",
            $"role:{role.Name}",
            $"permissions=[{string.Join(',', requestedKeys)}]",
            HttpContext.GetCorrelationId(),
            cancellationToken);

        return Ok(new { message = "Permissions updated." });
    }
}
