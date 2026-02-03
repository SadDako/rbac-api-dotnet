using Microsoft.AspNetCore.Http;

namespace Rbac.Api.Infrastructure.Http;

public static class HttpContextCorrelationExtensions
{
    public const string CorrelationHeaderName = "X-Correlation-Id";
    internal const string CorrelationItemName = "CorrelationId";

    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationItemName, out var value) && value is string correlationId && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return context.TraceIdentifier;
    }
}
