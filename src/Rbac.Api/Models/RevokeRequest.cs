using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Models;

public sealed class RevokeRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;

    [Required]
    public string DeviceFingerprint { get; init; } = string.Empty;
}
