using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Rbac.Api.Infrastructure.Http;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(HttpContext context, int statusCode, string code, string message)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = message,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["correlationId"] = context.GetCorrelationId();
        problem.Extensions["code"] = code;
        problem.Extensions["message"] = message;

        return problem;
    }

    public static (string Code, string Message) ResolveDefault(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => ("request.invalid", "The request could not be processed."),
            StatusCodes.Status401Unauthorized => ("auth.unauthorized", "Authentication is required for this resource."),
            StatusCodes.Status403Forbidden => ("rbac.forbidden", "You do not have permission to access this resource."),
            StatusCodes.Status404NotFound => ("resource.not_found", "The requested resource was not found."),
            StatusCodes.Status409Conflict => ("request.conflict", "The request conflicts with current data."),
            _ => ("server.error", "An unexpected error occurred.")
        };
    }

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        CancellationToken cancellationToken = default)
    {
        var payload = Create(context, statusCode, code, message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken);
    }
}
