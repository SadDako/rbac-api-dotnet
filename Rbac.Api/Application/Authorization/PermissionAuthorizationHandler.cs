using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Infrastructure.Data;

namespace Rbac.Api.Application.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(AppDbContext dbContext, ILogger<PermissionAuthorizationHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdValue =
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            _logger.LogWarning("Permission check failed: token user id is invalid.");
            return;
        }

        var normalizedPermission = requirement.Permission.Trim().ToLowerInvariant();

        var hasPermission = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                _dbContext.RolePermissions.AsNoTracking(),
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(
                _dbContext.Permissions.AsNoTracking(),
                permissionId => permissionId,
                permission => permission.Id,
                (_, permission) => permission.Key)
            .AnyAsync(key => key.ToLower() == normalizedPermission);

        if (hasPermission)
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogInformation(
            "Permission denied. userId={UserId} permission={Permission}",
            userId,
            requirement.Permission);
    }
}
