using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Models;
using Rbac.Application.Commands.Auth;
using Rbac.Domain;
using Rbac.Shared;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterUserCommand
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password
        }, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Created(string.Empty, result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var requestContext = CaptureSecurityContext();
        var result = await _sender.Send(new AuthenticateUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            DeviceFingerprint = request.DeviceFingerprint,
            UserAgent = requestContext.UserAgent,
            AcceptLanguage = requestContext.AcceptLanguage,
            IPAddress = requestContext.IPAddress,
            Timezone = requestContext.Timezone,
            Platform = requestContext.Platform,
            DeviceName = requestContext.DeviceName,
            Browser = requestContext.Browser,
            RelevantHeaders = requestContext.RelevantHeaders
        }, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var requestContext = CaptureSecurityContext();
        var result = await _sender.Send(new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            DeviceFingerprint = request.DeviceFingerprint,
            UserAgent = requestContext.UserAgent,
            AcceptLanguage = requestContext.AcceptLanguage,
            IPAddress = requestContext.IPAddress,
            Timezone = requestContext.Timezone,
            Platform = requestContext.Platform,
            DeviceName = requestContext.DeviceName,
            Browser = requestContext.Browser,
            RelevantHeaders = requestContext.RelevantHeaders
        }, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(result.Value);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeRequest request, CancellationToken cancellationToken)
    {
        var requestContext = CaptureSecurityContext();
        var result = await _sender.Send(new RevokeRefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            DeviceFingerprint = request.DeviceFingerprint,
            UserAgent = requestContext.UserAgent,
            AcceptLanguage = requestContext.AcceptLanguage,
            IPAddress = requestContext.IPAddress,
            Timezone = requestContext.Timezone,
            Platform = requestContext.Platform,
            DeviceName = requestContext.DeviceName,
            Browser = requestContext.Browser,
            RelevantHeaders = requestContext.RelevantHeaders
        }, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }

    private CapturedSecurityContext CaptureSecurityContext()
    {
        var headers = Request.Headers;
        var relevantHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[]
        {
            "Accept",
            "Accept-Encoding",
            "Accept-Language",
            "DNT",
            "Origin",
            "Referer",
            "Sec-CH-UA",
            "Sec-CH-UA-Mobile",
            "Sec-CH-UA-Platform",
            "X-Device-Fingerprint",
            "X-Device-Name",
            "X-Platform",
            "X-Timezone"
        })
        {
            if (headers.TryGetValue(name, out var value))
            {
                relevantHeaders[name] = value.ToString();
            }
        }

        return new CapturedSecurityContext
        {
            UserAgent = headers["User-Agent"].ToString(),
            AcceptLanguage = headers["Accept-Language"].ToString(),
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Timezone = headers["X-Timezone"].ToString(),
            Platform = headers["X-Platform"].FirstOrDefault()
                       ?? headers["Sec-CH-UA-Platform"].ToString().Trim('"')
                       ?? string.Empty,
            DeviceName = headers["X-Device-Name"].ToString(),
            Browser = headers["X-Browser"].ToString(),
            RelevantHeaders = relevantHeaders
        };
    }

    private sealed class CapturedSecurityContext
    {
        public string UserAgent { get; init; } = string.Empty;
        public string AcceptLanguage { get; init; } = string.Empty;
        public string IPAddress { get; init; } = string.Empty;
        public string Timezone { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;
        public string DeviceName { get; init; } = string.Empty;
        public string Browser { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, string> RelevantHeaders { get; init; } = new Dictionary<string, string>();
    }
}
