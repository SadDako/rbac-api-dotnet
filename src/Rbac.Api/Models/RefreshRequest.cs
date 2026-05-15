using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Models;

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;

    [Required]
    public string DeviceFingerprint { get; init; } = string.Empty;
}
