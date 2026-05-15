using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
