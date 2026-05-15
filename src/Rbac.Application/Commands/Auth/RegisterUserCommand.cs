using MediatR;
using Rbac.Application.Dtos;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class RegisterUserCommand : IRequest<Result<UserProfileDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
