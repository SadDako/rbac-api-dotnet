using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Application.Authorization;
using Rbac.Api.Contracts.Permissions;
using Rbac.Api.Infrastructure.Data;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("permissions")]
[Authorize]
[RequirePermission("permissions.read")]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public PermissionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> List(CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Key)
            .Select(p => new PermissionResponse(p.Id, p.Key, p.Description))
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }
}
