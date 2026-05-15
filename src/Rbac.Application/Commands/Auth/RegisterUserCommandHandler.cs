using MediatR;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserProfileDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result<UserProfileDto>.Failure("Email já está em uso.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            Name = request.Name.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        var defaultRole = await _roleRepository.GetByNameAsync("User", cancellationToken);
        if (defaultRole is null)
        {
            defaultRole = new Role { Name = "User", DisplayName = "Standard User" };
            await _roleRepository.AddAsync(defaultRole, cancellationToken);
        }

        user.UserRoles.Add(new UserRole { Role = defaultRole, User = user });
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<UserProfileDto>.Success(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray()
        });
    }
}
