using MediatR;
using Rbac.Application.Dtos;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class RefreshTokenCommand : IRequest<Result<AuthResponseDto>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string DeviceFingerprint { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public string AcceptLanguage { get; init; } = string.Empty;
    public string IPAddress { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string Browser { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> RelevantHeaders { get; init; } = new Dictionary<string, string>();
}
