using MediatR;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Shared;

namespace Rbac.Application.Queries.Users;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, Result<Pagination<UserProfileDto>>>
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Pagination<UserProfileDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.ListAsync(request.Page, request.PageSize, cancellationToken);
        var totalCount = await _userRepository.CountAsync(cancellationToken);

        var result = new Pagination<UserProfileDto>
        {
            Items = users.Select(user => new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray()
            }).ToArray(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Result<Pagination<UserProfileDto>>.Success(result);
    }
}
