using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Models;

public sealed class RegisterRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string DeviceFingerprint { get; init; } = string.Empty;
}
