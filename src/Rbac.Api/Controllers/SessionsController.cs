using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Application.Interfaces;
using Rbac.Domain.Entities;

namespace Rbac.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var sessions = await _sessionService.GetUserSessionsAsync(userId, cancellationToken);
        var result = sessions.Select(s => new
        {
            id = s.Id,
            deviceId = s.DeviceId,
            deviceName = s.DeviceName,
            browser = s.Browser,
            os = s.OS,
            ip = s.IPAddress,
            country = s.Country,
            city = s.City,
            userAgent = s.UserAgent,
            fingerprint = s.Fingerprint,
            isActive = s.IsActive,
            isRevoked = s.IsRevoked,
            isSuspicious = s.IsSuspicious,
            isCompromised = s.IsCompromised,
            requiresMfa = s.RequiresMfa,
            riskScore = s.RiskScore,
            createdAt = s.CreatedAtUtc,
            lastSeenAt = s.LastSeenAtUtc
        });

        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var fingerprint = Request.Headers["X-Device-Fingerprint"].ToString();
        if (string.IsNullOrWhiteSpace(fingerprint)) return BadRequest(new { message = "Missing X-Device-Fingerprint header" });

        var sessions = await _sessionService.GetUserSessionsAsync(userId, cancellationToken);
        var current = sessions.FirstOrDefault(s => s.Fingerprint == fingerprint);
        if (current is null) return NotFound();

        return Ok(new
        {
            id = current.Id,
            deviceId = current.DeviceId,
            deviceName = current.DeviceName,
            browser = current.Browser,
            os = current.OS,
            ip = current.IPAddress,
            country = current.Country,
            city = current.City,
            userAgent = current.UserAgent,
            fingerprint = current.Fingerprint,
            isActive = current.IsActive,
            isRevoked = current.IsRevoked,
            isSuspicious = current.IsSuspicious,
            isCompromised = current.IsCompromised,
            requiresMfa = current.RequiresMfa,
            riskScore = current.RiskScore,
            createdAt = current.CreatedAtUtc,
            lastSeenAt = current.LastSeenAtUtc
        });
    }

    [HttpGet("trusted-devices")]
    public async Task<IActionResult> TrustedDevices(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var devices = await _sessionService.GetTrustedDevicesAsync(userId, cancellationToken);
        return Ok(devices);
    }

    [HttpGet("suspicious")]
    public async Task<IActionResult> SuspiciousSessions(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var sessions = await _sessionService.GetSuspiciousSessionsAsync(userId, cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("active-threats")]
    public async Task<IActionResult> ActiveThreats(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var threats = await _sessionService.GetActiveThreatsAsync(userId, cancellationToken);
        return Ok(threats);
    }

    [HttpGet("device-history")]
    public async Task<IActionResult> DeviceHistory(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var devices = await _sessionService.GetDeviceHistoryAsync(userId, cancellationToken);
        return Ok(devices);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        // In a robust system validate that session belongs to user or admin privileges
        await _sessionService.RevokeSessionAsync(id, userId.ToString(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        await _sessionService.RevokeAllSessionsAsync(userId, userId.ToString(), cancellationToken);
        return NoContent();
    }
}
