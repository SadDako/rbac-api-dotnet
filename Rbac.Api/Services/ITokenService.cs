using Rbac.Api.Contracts.Auth;
using Rbac.Api.Domain.Entities;

namespace Rbac.Api.Services;

public interface ITokenService
{
    AuthResponse CreateAccessToken(User user);
    Task<(RefreshToken RefreshToken, string Token)> CreateRefreshTokenAsync(User user, string ipAddress);
    Task<AuthResponse> RefreshAccessTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken);
}
