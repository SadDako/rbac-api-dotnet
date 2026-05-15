using MediatR;
using Rbac.Application.Dtos;
using Rbac.Shared;

namespace Rbac.Application.Queries.Users;

public sealed class ListUsersQuery : IRequest<Result<Pagination<UserProfileDto>>> 
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
