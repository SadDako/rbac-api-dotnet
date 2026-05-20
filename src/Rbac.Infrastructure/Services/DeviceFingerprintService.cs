using System.Security.Cryptography;
using System.Text;
using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Services;

public sealed class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly IDeviceFingerprintRepository _repository;
    private readonly ISecurityEventService _securityEventService;

    public DeviceFingerprintService(IDeviceFingerprintRepository repository, ISecurityEventService securityEventService)
    {
        _repository = repository;
        _securityEventService = securityEventService;
    }

    public DeviceFingerprintRecord CreateDescriptor(SecurityRequestContext context, GeoLocationResult location)
    {
        var headerSignature = BuildHeaderSignature(context.RelevantHeaders);
        var fingerprint = ComputeFingerprint(
            context.ClientFingerprint,
            context.UserAgent,
            context.AcceptLanguage,
            context.Timezone,
            context.Platform,
            context.DeviceName,
            context.Browser,
            headerSignature);

        return new DeviceFingerprintRecord
        {
            Fingerprint = fingerprint,
            DeviceName = context.DeviceName,
            Browser = context.Browser,
            OS = context.Platform,
            IPAddress = context.IPAddress,
            Country = location.Country,
            City = location.City,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            UserAgent = context.UserAgent,
            AcceptLanguage = context.AcceptLanguage,
            Timezone = context.Timezone,
            Platform = context.Platform,
            HeaderSignature = headerSignature
        };
    }

    public async Task<(DeviceFingerprintRecord Record, bool IsNewDevice, bool IsSuspicious)> TrackDeviceAsync(Guid userId, DeviceFingerprintRecord descriptor, CancellationToken cancellationToken)
    {
        var fingerprint = descriptor.Fingerprint;
        var stored = await _repository.GetByUserAndFingerprintAsync(userId, fingerprint, cancellationToken);
        var isSuspicious = false;

        if (stored is null)
        {
            descriptor.UserId = userId;
            descriptor.FirstSeenAtUtc = DateTime.UtcNow;
            descriptor.LastSeenAtUtc = DateTime.UtcNow;
            descriptor.Occurrences = 1;
            descriptor.IsTrusted = false;
            descriptor.IsSuspicious = true;
            await _repository.AddAsync(descriptor, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                UserId = userId,
                Type = SecurityEventType.NewDevice,
                Severity = 50,
                RiskScore = 45,
                IPAddress = descriptor.IPAddress,
                Country = descriptor.Country,
                Device = descriptor.DeviceName,
                Description = "New device fingerprint detected.",
                Metadata = SerializeMetadata(descriptor)
            }, cancellationToken);

            return (descriptor, true, true);
        }

        var suspiciousChange = HasSuspiciousChange(stored, descriptor);
        stored.DeviceName = descriptor.DeviceName;
        stored.Browser = descriptor.Browser;
        stored.OS = descriptor.OS;
        stored.UserAgent = descriptor.UserAgent;
        stored.AcceptLanguage = descriptor.AcceptLanguage;
        stored.Timezone = descriptor.Timezone;
        stored.Platform = descriptor.Platform;
        stored.HeaderSignature = descriptor.HeaderSignature;
        stored.IPAddress = descriptor.IPAddress;
        stored.Country = descriptor.Country;
        stored.City = descriptor.City;
        stored.Latitude = descriptor.Latitude;
        stored.Longitude = descriptor.Longitude;
        stored.LastSeenAtUtc = DateTime.UtcNow;
        stored.Occurrences += 1;

        if (!stored.IsTrusted || suspiciousChange)
        {
            isSuspicious = true;
            stored.IsSuspicious = true;
            if (suspiciousChange)
            {
                stored.SuspiciousChangeDetectedAtUtc = DateTime.UtcNow;
                await _securityEventService.RecordEventAsync(new SecurityEvent
                {
                    UserId = userId,
                    Type = SecurityEventType.DeviceAnomaly,
                    Severity = 65,
                    RiskScore = 60,
                    IPAddress = descriptor.IPAddress,
                    Country = descriptor.Country,
                    Device = descriptor.DeviceName,
                    Description = "Device fingerprint metadata changed suspiciously.",
                    Metadata = SerializeMetadata(descriptor)
                }, cancellationToken);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return (stored, false, isSuspicious);
    }

    private static string SerializeMetadata(DeviceFingerprintRecord descriptor)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            descriptor.DeviceName,
            descriptor.Browser,
            descriptor.OS,
            descriptor.AcceptLanguage,
            descriptor.Timezone,
            descriptor.Platform,
            descriptor.HeaderSignature
        });
    }

    private static bool HasSuspiciousChange(DeviceFingerprintRecord stored, DeviceFingerprintRecord incoming)
    {
        if (stored.AcceptLanguage.Length > 0 && !string.Equals(stored.AcceptLanguage, incoming.AcceptLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (stored.Platform.Length > 0 && !string.Equals(stored.Platform, incoming.Platform, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return stored.HeaderSignature.Length > 0
               && !string.Equals(stored.HeaderSignature, incoming.HeaderSignature, StringComparison.Ordinal);
    }

    private static string BuildHeaderSignature(IReadOnlyDictionary<string, string> headers)
    {
        var material = string.Join("|", headers
            .OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
            .Select(h => $"{h.Key.ToLowerInvariant()}={Normalize(h.Value)}"));

        return ComputeHash(material);
    }

    private static string ComputeFingerprint(params string[] parts)
    {
        var material = string.Join("|", parts.Select(Normalize));
        return ComputeHash(material);
    }

    private static string ComputeHash(string value)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }
}
