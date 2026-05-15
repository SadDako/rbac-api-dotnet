namespace Rbac.Application.Dtos;

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = "Low";
    public bool RequiresMfa { get; init; }
}
