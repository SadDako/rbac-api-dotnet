using Rbac.Api.Domain.Entities;

namespace Rbac.Api.Application.Interfaces;

public interface IUserStore
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(Guid id);
    Task<bool> EmailExistsAsync(string email);
    Task CreateUserAsync(User user);

    Task EnsureRoleExistsAsync(string roleName);
    Task AddRoleToUserAsync(Guid userId, string roleName);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId);

    Task SeedAdminIfConfiguredAsync();
}
