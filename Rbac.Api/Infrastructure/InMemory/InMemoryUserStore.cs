using Microsoft.Extensions.Configuration;
using Rbac.Api.Application.Interfaces;
using Rbac.Api.Domain.Entities;

namespace Rbac.Api.Infrastructure.InMemory;

public class InMemoryUserStore : IUserStore
{
    private readonly IConfiguration _config;

    private static readonly List<User> Users = new();
    private static readonly List<Role> Roles = new();
    private static readonly List<UserRole> UserRoles = new();

    public InMemoryUserStore(IConfiguration config)
    {
        _config = config;
    }

    public Task<User?> FindByEmailAsync(string email)
    {
        var user = Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> FindByIdAsync(Guid id)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        var exists = Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task CreateUserAsync(User user)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task EnsureRoleExistsAsync(string roleName)
    {
        if (!Roles.Any(r => r.Name == roleName))
            Roles.Add(new Role { Id = Guid.NewGuid(), Name = roleName });

        return Task.CompletedTask;
    }

    public Task AddRoleToUserAsync(Guid userId, string roleName)
    {
        var role = Roles.FirstOrDefault(r => r.Name == roleName);
        if (role is null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName };
            Roles.Add(role);
        }

        var exists = UserRoles.Any(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (!exists)
            UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId)
    {
        var roleIds = UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToHashSet();
        var names = Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();
        return Task.FromResult((IReadOnlyList<string>)names);
    }

    public async Task SeedAdminIfConfiguredAsync()
    {
        await EnsureRoleExistsAsync("Admin");
        await EnsureRoleExistsAsync("User");

        var email = _config["Seed:Admin:Email"];
        var name = _config["Seed:Admin:Name"];
        var password = _config["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existing = await FindByEmailAsync(email);
        if (existing is not null) return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        await CreateUserAsync(admin);
        await AddRoleToUserAsync(admin.Id, "Admin");
    }
}
