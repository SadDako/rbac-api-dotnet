using Microsoft.Extensions.Primitives;

namespace Rbac.Api.Infrastructure.Http;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers);

        context.Items[HttpContextCorrelationExtensions.CorrelationItemName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HttpContextCorrelationExtensions.CorrelationHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["traceId"] = context.TraceIdentifier
        }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(HttpContextCorrelationExtensions.CorrelationHeaderName, out StringValues incoming)
            && !StringValues.IsNullOrEmpty(incoming))
        {
            var value = incoming.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
