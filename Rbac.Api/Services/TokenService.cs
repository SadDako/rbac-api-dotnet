using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rbac.Api.Contracts.Auth;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Options;

namespace Rbac.Api.Services;

public class TokenService : ITokenService
{
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public TokenService(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public AuthResponse CreateAccessToken(User user)
    {
        var expiresMinutes = _jwtOptions.ExpiresMinutes > 0 ? _jwtOptions.ExpiresMinutes : 15;

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), System.Security.Claims.ClaimValueTypes.Integer64)
        };

        foreach (var role in user.UserRoles.Select(ur => ur.Role.Name))
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new AuthResponse
        {
            AccessToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = token.ValidTo
        };
    }

    public async Task<(RefreshToken RefreshToken, string Token)> CreateRefreshTokenAsync(User user, string ipAddress)
    {
        var tokenValue = CreateSecureToken();
        var refreshToken = new RefreshToken
        {
            TokenHash = HashToken(tokenValue),
            UserId = user.Id,
            CreatedByIp = ipAddress,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiresDays > 0 ? _jwtOptions.RefreshTokenExpiresDays : 7),
            CreatedAtUtc = DateTime.UtcNow,
            RevokedAtUtc = null,
            RevokedByIp = null
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return (refreshToken, tokenValue);
    }

    public async Task<AuthResponse> RefreshAccessTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        var refreshTokenHash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new InvalidOperationException("Refresh token inválido ou expirado.");
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        var newRefreshTokenValue = CreateSecureToken();
        var newRefreshToken = new RefreshToken
        {
            TokenHash = HashToken(newRefreshTokenValue),
            UserId = storedToken.UserId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiresDays > 0 ? _jwtOptions.RefreshTokenExpiresDays : 7),
            CreatedAtUtc = DateTime.UtcNow,
            RevokedAtUtc = null,
            RevokedByIp = null
        };

        storedToken.ReplacedByTokenHash = newRefreshToken.TokenHash;
        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = storedToken.User;
        var accessToken = CreateAccessToken(user);
        accessToken.RefreshToken = newRefreshTokenValue;
        accessToken.RefreshTokenExpiresAtUtc = newRefreshToken.ExpiresAtUtc;
        return accessToken;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        var refreshTokenHash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return false;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }
}
