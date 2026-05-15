using Rbac.Application.Interfaces;
using Rbac.Application.Security;

namespace Rbac.Infrastructure.Services;

public sealed class RiskEngine : IRiskEngine
{
    public RiskAssessment Evaluate(RiskEvaluationContext context)
    {
        var signals = new List<string>();
        var score = 0;

        Add(context.IsNewDevice, 25, "new_device");
        Add(context.IsSuspiciousDevice, 20, "suspicious_device");
        Add(context.FingerprintMismatch, 45, "fingerprint_mismatch");
        Add(context.BruteForceDetected, 35, "brute_force");
        Add(context.MfaFailure, 20, "mfa_failure");
        Add(context.TokenReuseDetected, 90, "token_reuse");
        Add(context.SuspiciousIp, 25, "suspicious_ip");
        Add(context.ImpossibleTravel, 70, "impossible_travel");
        Add(context.RefreshAbuse, 30, "refresh_abuse");

        score = Math.Clamp(score, 0, 100);
        var level = score switch
        {
            <= 30 => RiskLevel.Low,
            <= 70 => RiskLevel.Suspicious,
            _ => RiskLevel.Critical
        };

        return new RiskAssessment
        {
            Score = score,
            Level = level,
            RequiresMfa = score >= 31,
            ShouldThrottle = context.BruteForceDetected || context.RefreshAbuse || score >= 31,
            ShouldRevoke = context.TokenReuseDetected || score >= 90,
            Signals = signals
        };

        void Add(bool condition, int points, string signal)
        {
            if (!condition)
            {
                return;
            }

            score += points;
            signals.Add(signal);
        }
    }
}
