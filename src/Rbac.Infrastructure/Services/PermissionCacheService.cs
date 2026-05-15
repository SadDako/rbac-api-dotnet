using System.Text.Json;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;
using Rbac.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Rbac.Infrastructure.Services;

public sealed class PermissionCacheService : IPermissionCache
{
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private const string CacheKeyFormat = "permissions:user:{0}";

    public PermissionCacheService(AppDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKeyFormat, userId);
        var cached = await _cacheService.GetAsync<IReadOnlyCollection<string>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var permissions = await _dbContext.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Concat(_dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.UserPermissions)
                .Select(up => up.Permission.Name))
            .Distinct()
            .ToArrayAsync(cancellationToken);

        await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(10), cancellationToken);
        return permissions;
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKeyFormat, userId);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}
