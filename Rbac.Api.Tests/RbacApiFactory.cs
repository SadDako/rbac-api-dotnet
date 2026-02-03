using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;

namespace Rbac.Api.Tests;

public class RbacApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var databaseName = $"rbac-tests-{Guid.NewGuid()}";

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Database.EnsureCreated();
            SeedTestData(dbContext);
        });
    }

    private static void SeedTestData(AppDbContext dbContext)
    {
        var adminRole = dbContext.Roles.FirstOrDefault(role => role.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin" };
            dbContext.Roles.Add(adminRole);
        }

        var userRole = dbContext.Roles.FirstOrDefault(role => role.Name == "User");
        if (userRole is null)
        {
            userRole = new Role { Name = "User" };
            dbContext.Roles.Add(userRole);
        }

        var permissionKeys = new[]
        {
            "admin.access",
            "users.me.read",
            "users.read",
            "users.roles.assign",
            "users.roles.remove",
            "roles.read",
            "roles.create",
            "roles.update",
            "roles.delete",
            "roles.permissions.update",
            "permissions.read",
            "activity.read",
            "activity.write"
        };

        foreach (var key in permissionKeys)
        {
            if (dbContext.Permissions.Any(permission => permission.Key == key))
            {
                continue;
            }

            dbContext.Permissions.Add(new Permission
            {
                Key = key,
                Description = $"Test permission {key}"
            });
        }

        dbContext.SaveChanges();

        var allPermissionIds = dbContext.Permissions.Select(permission => permission.Id).ToList();
        foreach (var permissionId in allPermissionIds)
        {
            if (dbContext.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permissionId))
            {
                continue;
            }

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permissionId
            });
        }

        var basicPermissionIds = dbContext.Permissions
            .Where(permission => permission.Key == "users.me.read" || permission.Key == "activity.read" || permission.Key == "activity.write")
            .Select(permission => permission.Id)
            .ToList();

        foreach (var permissionId in basicPermissionIds)
        {
            if (dbContext.RolePermissions.Any(rp => rp.RoleId == userRole.Id && rp.PermissionId == permissionId))
            {
                continue;
            }

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = userRole.Id,
                PermissionId = permissionId
            });
        }

        var adminUser = dbContext.Users.FirstOrDefault(user => user.Email == "admin@rbac.local");
        if (adminUser is null)
        {
            adminUser = new User
            {
                Name = "Admin",
                Email = "admin@rbac.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            };

            dbContext.Users.Add(adminUser);
            dbContext.SaveChanges();
        }

        if (!dbContext.UserRoles.Any(link => link.UserId == adminUser.Id && link.RoleId == adminRole.Id))
        {
            dbContext.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
        }

        dbContext.SaveChanges();
    }
}
