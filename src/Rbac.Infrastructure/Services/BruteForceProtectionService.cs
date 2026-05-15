using Rbac.Application.Interfaces;
using Rbac.Application.Security;
using Rbac.Domain.Entities;

namespace Rbac.Infrastructure.Services;

public sealed class BruteForceProtectionService : IBruteForceProtectionService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly IThreatCacheService _cache;
    private readonly ISecurityEventService _securityEventService;
    private readonly ISecurityMetrics _metrics;

    public BruteForceProtectionService(
        IThreatCacheService cache,
        ISecurityEventService securityEventService,
        ISecurityMetrics metrics)
    {
        _cache = cache;
        _securityEventService = securityEventService;
        _metrics = metrics;
    }

    public async Task<BruteForceAssessment> CheckAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken)
    {
        var lockKey = BuildLockKey(normalizedUserKey, context);
        if (await _cache.IsFlaggedAsync(lockKey, cancellationToken))
        {
            return new BruteForceAssessment
            {
                IsBlocked = true,
                RetryAfter = TimeSpan.FromMinutes(5),
                RiskScore = 65
            };
        }

        if (await _cache.IsFlaggedAsync($"suspicious-ip:{context.IPAddress}", cancellationToken))
        {
            return new BruteForceAssessment
            {
                IsBlocked = false,
                RetryAfter = TimeSpan.Zero,
                RiskScore = 35
            };
        }

        return new BruteForceAssessment();
    }

    public async Task<BruteForceAssessment> RecordFailureAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken)
    {
        var userCounter = await _cache.IncrementCounterAsync($"bruteforce:user:{normalizedUserKey}", Window, 5, cancellationToken);
        var ipCounter = await _cache.IncrementCounterAsync($"bruteforce:ip:{context.IPAddress}", Window, 20, cancellationToken);
        var fpCounter = await _cache.IncrementCounterAsync($"bruteforce:fp:{context.ClientFingerprint}", Window, 8, cancellationToken);
        var isBlocked = userCounter.IsLimitExceeded || fpCounter.IsLimitExceeded || ipCounter.IsLimitExceeded;
        var retryAfter = CalculateBackoff(Math.Max(userCounter.Count, fpCounter.Count));
        var credentialStuffing = ipCounter.IsLimitExceeded;

        _metrics.BruteForceAttempt();

        if (isBlocked)
        {
            await _cache.SetFlagAsync(BuildLockKey(normalizedUserKey, context), retryAfter, cancellationToken);
            if (credentialStuffing)
            {
                await _cache.SetFlagAsync($"suspicious-ip:{context.IPAddress}", TimeSpan.FromHours(6), cancellationToken);
            }

            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                Type = credentialStuffing ? SecurityEventType.CredentialStuffing : SecurityEventType.BruteForce,
                Severity = credentialStuffing ? 80 : 65,
                RiskScore = credentialStuffing ? 80 : 65,
                IPAddress = context.IPAddress,
                Device = context.DeviceName,
                Description = credentialStuffing
                    ? "Credential stuffing pattern detected by distributed counters."
                    : "Brute-force authentication pattern detected.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userCounter = userCounter.Count,
                    ipCounter = ipCounter.Count,
                    fingerprintCounter = fpCounter.Count,
                    retryAfterSeconds = retryAfter.TotalSeconds
                })
            }, cancellationToken);
        }

        return new BruteForceAssessment
        {
            IsBlocked = isBlocked,
            IsCredentialStuffing = credentialStuffing,
            IsDistributedAttack = ipCounter.IsLimitExceeded && fpCounter.Count <= 2,
            RetryAfter = retryAfter,
            RiskScore = isBlocked ? 65 : 25
        };
    }

    public async Task RecordSuccessAsync(string normalizedUserKey, SecurityRequestContext context, CancellationToken cancellationToken)
    {
        await _cache.ResetCounterAsync($"bruteforce:user:{normalizedUserKey}", cancellationToken);
        await _cache.ResetCounterAsync($"bruteforce:fp:{context.ClientFingerprint}", cancellationToken);
        await _cache.RemoveAsync(BuildLockKey(normalizedUserKey, context), cancellationToken);
    }

    private static string BuildLockKey(string normalizedUserKey, SecurityRequestContext context)
    {
        return $"lockout:{normalizedUserKey}:{context.IPAddress}:{context.ClientFingerprint}";
    }

    private static TimeSpan CalculateBackoff(long count)
    {
        if (count < 5)
        {
            return TimeSpan.Zero;
        }

        var exponent = Math.Min(count - 5, 8);
        return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, exponent) * 30, 900));
    }
}
