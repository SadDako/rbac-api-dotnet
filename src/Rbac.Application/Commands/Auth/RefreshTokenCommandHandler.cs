using MediatR;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken, new SecurityRequestContext
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
        return result;
    }
}
