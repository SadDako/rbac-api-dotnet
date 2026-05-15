using System.Diagnostics.Metrics;

namespace Rbac.Api.Infrastructure.Telemetry;

public static class TelemetryMetrics
{
    private static readonly Meter Meter = new("rbac.api.metrics", "1.0.0");

    public static readonly Counter<long> AuthSuccess = Meter.CreateCounter<long>("auth.success.count", description: "Successful authentications");
    public static readonly Counter<long> AuthFailure = Meter.CreateCounter<long>("auth.failure.count", description: "Failed authentications");
    public static readonly Counter<long> RefreshCount = Meter.CreateCounter<long>("auth.refresh.count", description: "Refresh token operations");
    public static readonly Counter<long> RateLimitExceeded = Meter.CreateCounter<long>("ratelimit.exceeded.count", description: "Rate limit rejections");
    public static readonly Counter<long> Exceptions = Meter.CreateCounter<long>("exceptions.count", description: "Unhandled exceptions");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("request.duration.ms", unit: "ms", description: "Request duration in milliseconds");
}
