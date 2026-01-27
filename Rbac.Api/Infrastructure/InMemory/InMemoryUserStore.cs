using Microsoft.Extensions.Configuration;
using Rbac.Api.Application.Interfaces;
using Rbac.Api.Domain.Entities;

namespace Rbac.Api.Infrastructure.InMemory;

public class InMemoryUserStore : IUserStore
{
    private readonly IConfiguration _config;

    private readonly List<User> _users = new();
    private readonly List<Role> _roles = new();
    private readonly List<UserRole> _userRoles = new();

    public InMemoryUserStore(IConfiguration config)
    {
        _config = config;
    }

    public Task<User?> FindByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> FindByIdAsync(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        var exists = _users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task CreateUserAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task EnsureRoleExistsAsync(string roleName)
    {
        if (!_roles.Any(r => r.Name == roleName))
            _roles.Add(new Role { Id = Guid.NewGuid(), Name = roleName });

        return Task.CompletedTask;
    }

    public Task AddRoleToUserAsync(Guid userId, string roleName)
    {
        var role = _roles.FirstOrDefault(r => r.Name == roleName);
        if (role is null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = roleName };
            _roles.Add(role);
        }

        var exists = _userRoles.Any(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (!exists)
            _userRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId)
    {
        var roleIds = _userRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToHashSet();
        var names = _roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();
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
