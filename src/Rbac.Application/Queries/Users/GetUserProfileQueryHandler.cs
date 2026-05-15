using MediatR;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Shared;

namespace Rbac.Application.Queries.Users;

public sealed class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserProfileDto>.Failure("Usuário não encontrado.");
        }

        return Result<UserProfileDto>.Success(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray()
        });
    }
}
