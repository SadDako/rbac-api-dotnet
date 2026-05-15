using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Api.Middleware;

public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditService _auditService;
    private const int MaxPayloadLength = 2000;

    public AuditMiddleware(RequestDelegate next, IAuditService auditService)
    {
        _next = next;
        _auditService = auditService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.Request.Headers.ContainsKey("X-Correlation-ID") ? context.Request.Headers["X-Correlation-ID"].ToString() : string.Empty;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Read request body safely
        var requestPayload = await ReadRequestBodyAsync(context.Request);
        requestPayload = MaskSensitive(requestPayload);

        // Swap out response body to capture it
        var originalBody = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        int statusCode = 0;
        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception ex)
        {
            statusCode = 500;
            // track exception metric via Telemetry if available
            throw;
        }
        finally
        {
            sw.Stop();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var respText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // copy back to original stream
            await responseBody.CopyToAsync(originalBody);

            var audit = new AuditLog
            {
                Action = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty,
                Endpoint = context.Request.Path,
                HttpMethod = context.Request.Method,
                StatusCode = statusCode,
                IpAddress = ip,
                UserAgent = userAgent,
                CorrelationId = correlationId,
                Payload = Truncate(MaskSensitive(requestPayload ?? string.Empty)),
                CreatedAtUtc = DateTime.UtcNow
            };

            if (Guid.TryParse(userId, out var uid))
            {
                audit.UserId = uid;
            }

            try
            {
                await _auditService.TrackAsync(audit, CancellationToken.None);
            }
            catch
            {
                // swallow to avoid interfering with the request pipeline
            }
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
        catch
        {
            return null;
        }
    }

    private static string Truncate(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= MaxPayloadLength ? s : s.Substring(0, MaxPayloadLength);
    }

    private static string MaskSensitive(string? payload)
    {
        if (string.IsNullOrEmpty(payload)) return string.Empty;
        // Basic JSON field masking for common sensitive fields
        try
        {
            var text = payload;
            var keys = new[] { "password", "pass", "pwd", "token", "access_token", "refresh_token", "authorization" };
            foreach (var key in keys)
            {
                // naive masking: replace "key":"value"
                text = System.Text.RegularExpressions.Regex.Replace(text,
                    $"(\"{key}\"\\s*:\\s*\")(.*?)(\")",
                    "$1***masked***$3",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return text;
        }
        catch
        {
            return payload;
        }
    }
}
