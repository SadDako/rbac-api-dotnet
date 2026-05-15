using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Domain.Entities;
using Rbac.Shared;
using Rbac.Shared.Options;

namespace Rbac.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPermissionCache _permissionCache;
    private readonly ISessionService _sessionService;
    private readonly ISecurityEventService _securityEventService;
    private readonly IThreatCacheService _threatCache;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IRiskEngine _riskEngine;
    private readonly JwtOptions _jwtOptions;

    public TokenService(
        IRefreshTokenRepository refreshTokenRepository,
        IPermissionCache permissionCache,
        ISessionService sessionService,
        ISecurityEventService securityEventService,
        IThreatCacheService threatCache,
        IDeviceFingerprintService deviceFingerprintService,
        IGeoLocationService geoLocationService,
        IRiskEngine riskEngine,
        IOptions<JwtOptions> jwtOptions)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _permissionCache = permissionCache;
        _sessionService = sessionService;
        _securityEventService = securityEventService;
        _threatCache = threatCache;
        _deviceFingerprintService = deviceFingerprintService;
        _geoLocationService = geoLocationService;
        _riskEngine = riskEngine;
        _jwtOptions = jwtOptions.Value;
    }

    public AuthResponseDto CreateAccessToken(User user)
    {
        ValidateJwtOptions();

        var permissions = _permissionCache.GetPermissionsForUserAsync(user.Id, CancellationToken.None).GetAwaiter().GetResult();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(user.UserRoles.Select(role => new Claim(ClaimTypes.Role, role.Role.Name)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiresMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt
        };
    }

    public async Task<(string RefreshToken, DateTime ExpiresAtUtc)> CreateRefreshTokenAsync(
        User user,
        SecurityRequestContext context,
        DeviceFingerprintRecord device,
        RiskAssessment risk,
        CancellationToken cancellationToken)
    {
        ValidateJwtOptions();

        var refreshTokenValue = GenerateSecureToken();
        var tokenFamilyId = Guid.NewGuid();
        var refreshToken = new RefreshToken
        {
            TokenHash = ComputeHash(refreshTokenValue),
            UserId = user.Id,
            CreatedByIp = context.IPAddress,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiresDays),
            DeviceFingerprint = device.Fingerprint,
            TokenFamilyId = tokenFamilyId
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenId = refreshToken.Id,
            DeviceId = device.Fingerprint,
            Fingerprint = device.Fingerprint,
            IPAddress = context.IPAddress,
            UserAgent = context.UserAgent,
            DeviceName = context.DeviceName,
            Browser = context.Browser,
            OS = context.Platform,
            Country = device.Country,
            City = device.City,
            Latitude = device.Latitude,
            Longitude = device.Longitude,
            IsActive = true,
            IsSuspicious = risk.Level != RiskLevel.Low,
            RequiresMfa = risk.RequiresMfa,
            RiskScore = risk.Score,
            TokenFamilyId = tokenFamilyId,
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        };

        await _sessionService.CreateSessionAsync(session, cancellationToken);
        return (refreshTokenValue, refreshToken.ExpiresAtUtc);
    }

    public async Task<Result<AuthResponseDto>> RefreshAccessTokenAsync(string refreshToken, SecurityRequestContext context, CancellationToken cancellationToken)
    {
        ValidateJwtOptions();

        var tokenHash = ComputeHash(refreshToken);
        if (await _threatCache.IsFlaggedAsync($"token:blacklist:{tokenHash}", cancellationToken))
        {
            return Result<AuthResponseDto>.Failure("Refresh token invalido ou expirado.");
        }

        var refreshLock = await _threatCache.TryAcquireLockAsync($"refresh:{tokenHash}", TimeSpan.FromSeconds(10), cancellationToken);
        if (refreshLock is null)
        {
            return Result<AuthResponseDto>.Failure("Refresh token ja esta sendo processado.");
        }

        await using var _ = refreshLock;
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null)
        {
            return Result<AuthResponseDto>.Failure("Refresh token invalido ou expirado.");
        }

        var location = await _geoLocationService.LocateAsync(context.IPAddress, cancellationToken);
        var descriptor = _deviceFingerprintService.CreateDescriptor(context, location);
        var fingerprintMismatch = !string.Equals(storedToken.DeviceFingerprint, descriptor.Fingerprint, StringComparison.Ordinal);
        var refreshCounter = await _threatCache.IncrementCounterAsync($"refresh:user:{storedToken.UserId}", TimeSpan.FromMinutes(10), 20, cancellationToken);
        var refreshAbuse = refreshCounter.IsLimitExceeded;

        if (storedToken.IsUsed || storedToken.IsRevoked || storedToken.IsCompromised)
        {
            await HandleTokenReuseAsync(storedToken, context, descriptor, cancellationToken);
            return Result<AuthResponseDto>.Failure("Refresh token reutilizado. Sessoes comprometidas foram revogadas.");
        }

        if (!storedToken.IsActive || storedToken.IsExpired)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            storedToken.RevokedByIp = context.IPAddress;
            await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return Result<AuthResponseDto>.Failure("Refresh token expirado.");
        }

        if (fingerprintMismatch)
        {
            await _sessionService.MarkRefreshTokenSessionSuspiciousAsync(storedToken.Id, "fingerprint_mismatch", cancellationToken);
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                UserId = storedToken.UserId,
                Type = SecurityEventType.FingerprintMismatch,
                Severity = 75,
                RiskScore = 75,
                IPAddress = context.IPAddress,
                Country = location.Country,
                Device = context.DeviceName,
                Description = "Refresh token fingerprint mismatch detected.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    expected = storedToken.DeviceFingerprint,
                    actual = descriptor.Fingerprint,
                    context.UserAgent,
                    context.AcceptLanguage
                })
            }, cancellationToken);
            return Result<AuthResponseDto>.Failure("Refresh token invalido para este dispositivo.");
        }

        var impossibleTravel = await DetectImpossibleTravelAsync(storedToken.UserId, context, location, cancellationToken);
        var suspiciousIp = await _threatCache.IsFlaggedAsync($"suspicious-ip:{context.IPAddress}", cancellationToken);
        var risk = _riskEngine.Evaluate(new RiskEvaluationContext
        {
            UserId = storedToken.UserId,
            Request = context,
            Location = location,
            SuspiciousIp = suspiciousIp,
            ImpossibleTravel = impossibleTravel,
            RefreshAbuse = refreshAbuse
        });

        if (risk.RequiresMfa)
        {
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                UserId = storedToken.UserId,
                Type = refreshAbuse ? SecurityEventType.ExcessiveRefresh : SecurityEventType.SuspiciousLogin,
                Severity = risk.Score,
                RiskScore = risk.Score,
                IPAddress = context.IPAddress,
                Country = location.Country,
                Device = context.DeviceName,
                Description = "Adaptive risk detected during refresh token rotation.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { risk.Signals, risk.RequiresMfa })
            }, cancellationToken);
        }

        storedToken.IsUsed = true;
        storedToken.UsedAtUtc = DateTime.UtcNow;
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = context.IPAddress;

        var newRefreshTokenValue = GenerateSecureToken();
        var newTokenHash = ComputeHash(newRefreshTokenValue);
        var newRefreshToken = new RefreshToken
        {
            TokenHash = newTokenHash,
            UserId = storedToken.UserId,
            CreatedByIp = context.IPAddress,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiresDays),
            DeviceFingerprint = descriptor.Fingerprint,
            ParentTokenHash = storedToken.TokenHash,
            TokenFamilyId = storedToken.TokenFamilyId
        };
        storedToken.ReplacedByTokenHash = newTokenHash;

        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var accessToken = CreateAccessToken(storedToken.User);
        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = newRefreshTokenValue,
            RefreshTokenExpiresAtUtc = newRefreshToken.ExpiresAtUtc,
            RiskScore = risk.Score,
            RiskLevel = risk.Level.ToString(),
            RequiresMfa = risk.RequiresMfa
        });
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, SecurityRequestContext context, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeHash(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        var location = await _geoLocationService.LocateAsync(context.IPAddress, cancellationToken);
        var descriptor = _deviceFingerprintService.CreateDescriptor(context, location);
        if (storedToken is null || storedToken.IsRevoked || storedToken.DeviceFingerprint != descriptor.Fingerprint)
        {
            return Result.Failure("Refresh token nao encontrado ou ja revogado.");
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = "revoked";
        await _threatCache.SetFlagAsync($"token:blacklist:{storedToken.TokenHash}", TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiresDays), cancellationToken);
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task HandleTokenReuseAsync(RefreshToken storedToken, SecurityRequestContext context, DeviceFingerprintRecord descriptor, CancellationToken cancellationToken)
    {
        var family = (await _refreshTokenRepository.ListByFamilyAsync(storedToken.TokenFamilyId, cancellationToken)).ToList();
        var now = DateTime.UtcNow;
        foreach (var token in family)
        {
            token.IsCompromised = true;
            token.CompromisedAtUtc ??= now;
            token.ReuseDetectedAtUtc ??= now;
            token.RevokedAtUtc ??= now;
            token.RevokedByIp = context.IPAddress;
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
            await _threatCache.SetFlagAsync($"token:blacklist:{token.TokenHash}", TimeSpan.FromDays(_jwtOptions.RefreshTokenExpiresDays), cancellationToken);
        }

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        await _sessionService.CompromiseSessionsAsync(storedToken.UserId, family.Select(t => t.Id), "refresh_token_reuse", cancellationToken);
        await _threatCache.SetFlagAsync($"compromised-session:user:{storedToken.UserId}", TimeSpan.FromDays(1), cancellationToken);

        await _securityEventService.RecordEventAsync(new SecurityEvent
        {
            UserId = storedToken.UserId,
            Type = SecurityEventType.TokenReuse,
            Severity = 100,
            RiskScore = 100,
            IPAddress = context.IPAddress,
            Country = descriptor.Country,
            Device = context.DeviceName,
            Description = "Critical refresh token reuse detected. Token family and associated sessions revoked.",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                storedToken.TokenFamilyId,
                tokenCount = family.Count,
                descriptor.Fingerprint
            })
        }, cancellationToken);
    }

    private async Task<bool> DetectImpossibleTravelAsync(Guid userId, SecurityRequestContext context, GeoLocationResult location, CancellationToken cancellationToken)
    {
        if (location.Latitude is null || location.Longitude is null)
        {
            return false;
        }

        var sessions = await _sessionService.GetUserSessionsAsync(userId, cancellationToken);
        var previous = sessions
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue && s.IPAddress != context.IPAddress)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .FirstOrDefault();

        if (previous is null)
        {
            return false;
        }

        var minutes = Math.Max((DateTime.UtcNow - previous.LastSeenAtUtc).TotalMinutes, 1);
        var distanceKm = Haversine(previous.Latitude!.Value, previous.Longitude!.Value, location.Latitude.Value, location.Longitude.Value);
        var speedKmh = distanceKm / (minutes / 60);
        var impossible = distanceKm > 500 && speedKmh > 900;

        if (impossible)
        {
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                UserId = userId,
                Type = SecurityEventType.ImpossibleTravel,
                Severity = 90,
                RiskScore = 90,
                IPAddress = context.IPAddress,
                Country = location.Country,
                Device = context.DeviceName,
                Description = "Impossible travel detected during refresh flow.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    from = new { previous.Country, previous.City, previous.Latitude, previous.Longitude, previous.LastSeenAtUtc },
                    to = new { location.Country, location.City, location.Latitude, location.Longitude },
                    distanceKm,
                    speedKmh
                })
            }, cancellationToken);
        }

        return impossible;
    }

    private void ValidateJwtOptions()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key)
            || string.IsNullOrWhiteSpace(_jwtOptions.Issuer)
            || string.IsNullOrWhiteSpace(_jwtOptions.Audience))
        {
            throw new InvalidOperationException("JWT settings are incompletos.");
        }

        if (Encoding.UTF8.GetByteCount(_jwtOptions.Key) < 32)
        {
            throw new InvalidOperationException("Chave JWT deve conter pelo menos 32 bytes.");
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string value)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
