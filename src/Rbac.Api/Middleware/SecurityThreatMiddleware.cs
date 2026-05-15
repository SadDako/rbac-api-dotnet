using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Api.Middleware;

public sealed class SecurityThreatMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IThreatCacheService _threatCache;
    private readonly ISecurityEventService _securityEventService;

    public SecurityThreatMiddleware(
        RequestDelegate next,
        IThreatCacheService threatCache,
        ISecurityEventService securityEventService)
    {
        _next = next;
        _threatCache = threatCache;
        _securityEventService = securityEventService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
        var score = 0;
        var signals = new List<string>();

        if (string.IsNullOrWhiteSpace(userAgent) && IsAuthPath(context.Request.Path))
        {
            score += 20;
            signals.Add("missing_user_agent");
        }

        if (HasSuspiciousForwardedHeaders(context.Request.Headers))
        {
            score += 25;
            signals.Add("suspicious_forwarded_headers");
        }

        if (HasMalformedBearerToken(context.Request.Headers["Authorization"].ToString()))
        {
            score += 50;
            signals.Add("malformed_bearer_token");
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.MalformedToken,
                Severity = 50,
                RiskScore = 50,
                IPAddress = ipAddress,
                Device = userAgent,
                Description = "Malformed bearer token intercepted by security middleware.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { correlationId })
            }, cancellationToken);
        }

        if (context.Request.Path.StartsWithSegments("/api/v1/auth/refresh"))
        {
            var refreshCounter = await _threatCache.IncrementCounterAsync($"refresh:ip:{ipAddress}", TimeSpan.FromMinutes(10), 60, cancellationToken);
            if (refreshCounter.IsLimitExceeded)
            {
                score += 40;
                signals.Add("abnormal_refresh_pattern");
                await _threatCache.SetFlagAsync($"suspicious-ip:{ipAddress}", TimeSpan.FromHours(2), cancellationToken);
            }
        }

        if (signals.Count > 0)
        {
            Activity.Current?.SetTag("security.risk_score", score);
            Activity.Current?.SetTag("security.signals", string.Join(",", signals));
            context.Response.Headers["X-Adaptive-Risk"] = score.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (score >= 70)
        {
            await _securityEventService.RecordEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.SuspiciousHeaders,
                Severity = score,
                RiskScore = score,
                IPAddress = ipAddress,
                Device = userAgent,
                Description = "Suspicious request blocked by adaptive security middleware.",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { correlationId, signals })
            }, cancellationToken);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { message = "Request rejected by adaptive security policy." }, cancellationToken);
            return;
        }

        await _next(context);
    }

    private static bool IsAuthPath(PathString path)
    {
        return path.StartsWithSegments("/api/v1/auth");
    }

    private static bool HasSuspiciousForwardedHeaders(IHeaderDictionary headers)
    {
        var forwardedFor = headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(forwardedFor))
        {
            return false;
        }

        return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 5;
    }

    private static bool HasMalformedBearerToken(string authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorization["Bearer ".Length..].Trim();
        return token.Length > 0 && !new JwtSecurityTokenHandler().CanReadToken(token);
    }
}
