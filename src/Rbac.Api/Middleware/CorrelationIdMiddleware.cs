using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Rbac.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderKey = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderKey, out var correlation))
        {
            correlation = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderKey] = correlation;
        }

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderKey))
            {
                context.Response.Headers.Append(HeaderKey, correlation.ToString());
            }
            return Task.CompletedTask;
        });

        // Propagate to Activity and Trace
        var activity = Activity.Current ?? new Activity("request");
        if (string.IsNullOrEmpty(activity.TraceId.ToString()) || activity.Id == null)
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
        }

        activity.SetTag("correlation_id", correlation.ToString());

        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                if (activity != null && activity.Duration == TimeSpan.Zero)
                {
                    activity.Stop();
                }
            }
            catch { }
        }
    }
}
