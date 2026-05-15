using Rbac.Application.Dtos;
using Rbac.Application.Security;
using Rbac.Domain.Entities;
using Rbac.Shared;

namespace Rbac.Application.Interfaces;

public interface ITokenService
{
    AuthResponseDto CreateAccessToken(User user);
    Task<(string RefreshToken, DateTime ExpiresAtUtc)> CreateRefreshTokenAsync(User user, SecurityRequestContext context, DeviceFingerprintRecord device, RiskAssessment risk, CancellationToken cancellationToken);
    Task<Result<AuthResponseDto>> RefreshAccessTokenAsync(string refreshToken, SecurityRequestContext context, CancellationToken cancellationToken);
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, SecurityRequestContext context, CancellationToken cancellationToken);
}
