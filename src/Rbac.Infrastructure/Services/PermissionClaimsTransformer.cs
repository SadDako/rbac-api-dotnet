using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Rbac.Application.Interfaces;

namespace Rbac.Infrastructure.Services;

public sealed class PermissionClaimsTransformer : IClaimsTransformation
{
    private readonly IPermissionCache _permissionCache;

    public PermissionClaimsTransformer(IPermissionCache permissionCache)
    {
        _permissionCache = permissionCache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (!principal.Identity?.IsAuthenticated ?? true)
        {
            return principal;
        }

        if (principal.Claims.Any(c => c.Type == "permission"))
        {
            return principal;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userId, out var id))
        {
            return principal;
        }

        var permissions = await _permissionCache.GetPermissionsForUserAsync(id, CancellationToken.None);
        if (!permissions.Any())
        {
            return principal;
        }

        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        return principal;
    }
}
