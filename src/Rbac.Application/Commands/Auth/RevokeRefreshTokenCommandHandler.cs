using MediatR;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    private readonly ITokenService _tokenService;

    public RevokeRefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, new SecurityRequestContext
        {
            ClientFingerprint = request.DeviceFingerprint,
            UserAgent = request.UserAgent,
            AcceptLanguage = request.AcceptLanguage,
            IPAddress = string.IsNullOrWhiteSpace(request.IPAddress) ? "unknown" : request.IPAddress,
            Timezone = request.Timezone,
            Platform = request.Platform,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            RelevantHeaders = request.RelevantHeaders
        }, cancellationToken);
    }
}
