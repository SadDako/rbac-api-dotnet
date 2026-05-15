namespace Rbac.Shared.Options;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessTokenExpiresMinutes { get; set; } = 15;
    public int RefreshTokenExpiresDays { get; set; } = 7;
    public int ClockSkewSeconds { get; set; } = 60;
}
