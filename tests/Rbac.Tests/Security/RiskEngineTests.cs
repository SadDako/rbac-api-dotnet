using FluentAssertions;
using Rbac.Application.Security;
using Rbac.Infrastructure.Services;
using Xunit;

namespace Rbac.Tests.Security;

public sealed class RiskEngineTests
{
    [Fact]
    public void Evaluate_ShouldMarkTokenReuseAsCritical()
    {
        var engine = new RiskEngine();

        var result = engine.Evaluate(new RiskEvaluationContext
        {
            TokenReuseDetected = true,
            Request = new SecurityRequestContext { IPAddress = "203.0.113.10" }
        });

        result.Score.Should().Be(90);
        result.Level.Should().Be(RiskLevel.Critical);
        result.ShouldRevoke.Should().BeTrue();
        result.RequiresMfa.Should().BeTrue();
        result.Signals.Should().Contain("token_reuse");
    }

    [Fact]
    public void Evaluate_ShouldScoreNewSuspiciousDeviceWithinSuspiciousRange()
    {
        var engine = new RiskEngine();

        var result = engine.Evaluate(new RiskEvaluationContext
        {
            IsNewDevice = true,
            IsSuspiciousDevice = true
        });

        result.Score.Should().Be(45);
        result.Level.Should().Be(RiskLevel.Suspicious);
        result.RequiresMfa.Should().BeTrue();
    }
}
