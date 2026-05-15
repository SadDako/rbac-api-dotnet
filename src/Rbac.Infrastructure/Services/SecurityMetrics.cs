using System.Diagnostics.Metrics;
using Rbac.Application.Interfaces;

namespace Rbac.Infrastructure.Services;

public sealed class SecurityMetrics : ISecurityMetrics
{
    private static readonly Meter Meter = new("rbac.security.metrics", "1.0.0");

    private static readonly Counter<long> SuspiciousLogins = Meter.CreateCounter<long>("suspicious_logins_total");
    private static readonly Counter<long> BruteForceAttempts = Meter.CreateCounter<long>("brute_force_attempts_total");
    private static readonly Counter<long> TokenReuseTotal = Meter.CreateCounter<long>("token_reuse_total");
    private static readonly Counter<long> ImpossibleTravelTotal = Meter.CreateCounter<long>("impossible_travel_total");
    private static readonly Counter<long> SuspiciousDevicesTotal = Meter.CreateCounter<long>("suspicious_devices_total");
    private static readonly Counter<long> CompromisedSessionsTotal = Meter.CreateCounter<long>("compromised_sessions_total");
    private static readonly Counter<long> AdaptiveMfaTotal = Meter.CreateCounter<long>("adaptive_mfa_total");

    public void SuspiciousLogin() => SuspiciousLogins.Add(1);
    public void BruteForceAttempt() => BruteForceAttempts.Add(1);
    public void TokenReuse() => TokenReuseTotal.Add(1);
    public void ImpossibleTravel() => ImpossibleTravelTotal.Add(1);
    public void SuspiciousDevice() => SuspiciousDevicesTotal.Add(1);
    public void CompromisedSession() => CompromisedSessionsTotal.Add(1);
    public void AdaptiveMfa() => AdaptiveMfaTotal.Add(1);
}
