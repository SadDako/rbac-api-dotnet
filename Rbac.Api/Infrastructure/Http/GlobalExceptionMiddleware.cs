namespace Rbac.Api.Infrastructure.Http;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path}. traceId={TraceId} correlationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                context.GetCorrelationId());

            if (context.Response.HasStarted)
            {
                throw;
            }

            await ApiProblemDetailsFactory.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "server.unhandled_exception",
                "An unexpected server error happened. Try again later.",
                context.RequestAborted);
        }
    }
}
