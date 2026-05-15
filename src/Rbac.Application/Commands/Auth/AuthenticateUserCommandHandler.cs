using MediatR;
using Rbac.Application.Dtos;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Domain.Entities;
using Rbac.Shared;

namespace Rbac.Application.Commands.Auth;

public sealed class AuthenticateUserCommandHandler : IRequestHandler<AuthenticateUserCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IBruteForceProtectionService _bruteForceProtectionService;
    private readonly IThreatCacheService _threatCache;
    private readonly IRiskEngine _riskEngine;
    private readonly ISecurityEventService _securityEventService;
    private readonly ISessionService _sessionService;

    public AuthenticateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDeviceFingerprintService deviceFingerprintService,
        IGeoLocationService geoLocationService,
        IBruteForceProtectionService bruteForceProtectionService,
        IThreatCacheService threatCache,
        IRiskEngine riskEngine,
        ISecurityEventService securityEventService,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _deviceFingerprintService = deviceFingerprintService;
        _geoLocationService = geoLocationService;
        _bruteForceProtectionService = bruteForceProtectionService;
        _threatCache = threatCache;
        _riskEngine = riskEngine;
        _securityEventService = securityEventService;
        _sessionService = sessionService;
    }

    public async Task<Result<AuthResponseDto>> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var context = BuildSecurityContext(request);
        var bruteForceCheck = await _bruteForceProtectionService.CheckAsync(normalizedEmail, context, cancellationToken);
        if (bruteForceCheck.IsBlocked)
        {
            return Result<AuthResponseDto>.Failure("Login temporariamente bloqueado por risco de brute-force.");
        }

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _bruteForceProtectionService.RecordFailureAsync(normalizedEmail, context, cancellationToken);
            return Result<AuthResponseDto>.Failure("Credenciais invalidas.");
        }

        if (user.LockedOut && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            return Result<AuthResponseDto>.Failure("Conta temporariamente bloqueada por tentativas invalidas.");
        }

        await _bruteForceProtectionService.RecordSuccessAsync(normalizedEmail, context, cancellationToken);

        var location = await _geoLocationService.LocateAsync(context.IPAddress, cancellationToken);
        var deviceDescriptor = _deviceFingerprintService.CreateDescriptor(context, location);
        var deviceTracking = await _deviceFingerprintService.TrackDeviceAsync(user.Id, deviceDescriptor, cancellationToken);
        var suspiciousIp = await _threatCache.IsFlaggedAsync($"suspicious-ip:{context.IPAddress}", cancellationToken);
        var impossibleTravel = await DetectImpossibleTravelAsync(user.Id, context, location, cancellationToken);
        var risk = _riskEngine.Evaluate(new RiskEvaluationContext
        {
            UserId = user.Id,
            Request = context,
            Location = location,
            IsNewDevice = deviceTracking.IsNewDevice,
            IsSuspiciousDevice = deviceTracking.IsSuspicious,
            SuspiciousIp = suspiciousIp,
            ImpossibleTravel = impossibleTravel,
            BruteForceDetected = bruteForceCheck.RiskScore >= 35
        });

        if (risk.Level != RiskLevel.Low)
        {
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                UserId = user.Id,
                Type = SecurityEventType.SuspiciousLogin,
                Severity = risk.Score,
                RiskScore = risk.Score,
                IPAddress = context.IPAddress,
                Country = location.Country,
                Device = context.DeviceName,
                Description = "Adaptive risk detected during authentication.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { risk.Signals, risk.RequiresMfa })
            }, cancellationToken);
        }

        var accessToken = _tokenService.CreateAccessToken(user);
        var (refreshToken, expiresAtUtc) = await _tokenService.CreateRefreshTokenAsync(user, context, deviceTracking.Record, risk, cancellationToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = expiresAtUtc,
            RiskScore = risk.Score,
            RiskLevel = risk.Level.ToString(),
            RequiresMfa = risk.RequiresMfa
        });
    }

    private static SecurityRequestContext BuildSecurityContext(AuthenticateUserCommand request)
    {
        return new SecurityRequestContext
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
        };
    }

    private async Task<bool> DetectImpossibleTravelAsync(Guid userId, SecurityRequestContext context, GeoLocationResult location, CancellationToken cancellationToken)
    {
        if (location.Latitude is null || location.Longitude is null)
        {
            return false;
        }

        var previous = (await _sessionService.GetUserSessionsAsync(userId, cancellationToken))
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
        if (!impossible)
        {
            return false;
        }

        await _securityEventService.RecordEventAsync(new SecurityEvent
        {
            UserId = userId,
            Type = SecurityEventType.ImpossibleTravel,
            Severity = 90,
            RiskScore = 90,
            IPAddress = context.IPAddress,
            Country = location.Country,
            Device = context.DeviceName,
            Description = "Impossible travel detected during authentication.",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                from = new { previous.Country, previous.City, previous.Latitude, previous.Longitude, previous.LastSeenAtUtc },
                to = new { location.Country, location.City, location.Latitude, location.Longitude },
                distanceKm,
                speedKmh
            })
        }, cancellationToken);

        return true;
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
