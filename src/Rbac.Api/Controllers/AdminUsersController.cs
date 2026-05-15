using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Application.Interfaces;
using Rbac.Domain;
using Rbac.Domain.Entities;
using Rbac.Shared;

namespace Rbac.Api.Controllers;

[ApiController]
[Authorize(Policy = Permissions.RolesWrite)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public AdminUsersController(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    [HttpPost("{userId:guid}/promote-admin")]
    public async Task<IActionResult> Promote(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        var adminRole = await _roleRepository.GetByNameAsync("Admin", cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin", DisplayName = "Administrator" };
            await _roleRepository.AddAsync(adminRole, cancellationToken);
            await _roleRepository.SaveChangesAsync(cancellationToken);
        }

        if (user.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
        {
            return Ok(new { message = "Usuário já é Admin." });
        }

        user.UserRoles.Add(new UserRole { User = user, Role = adminRole });
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Usuário promovido para Admin.", userId });
    }

    [HttpPost("{userId:guid}/demote-admin")]
    public async Task<IActionResult> Demote(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        var adminRole = await _roleRepository.GetByNameAsync("Admin", cancellationToken);
        if (adminRole is null)
        {
            return NotFound(new { message = "Role Admin não existe." });
        }

        var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == adminRole.Id);
        if (userRole is null)
        {
            return Ok(new { message = "Usuário não é Admin." });
        }

        user.UserRoles.Remove(userRole);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Admin removido do usuário.", userId });
    }
}
