using MediatR;
using Rbac.Application.Dtos;
using Rbac.Shared;

namespace Rbac.Application.Queries.Users;

public sealed class GetUserProfileQuery : IRequest<Result<UserProfileDto>>
{
    public Guid UserId { get; init; }
}
