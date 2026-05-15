using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rbac.Application.Interfaces;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("api/v1/mfa")]
[Authorize]
public sealed class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;

    public MfaController(IMfaService mfaService)
    {
        _mfaService = mfaService;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var (secret, provisioningUri, qrBase64) = await _mfaService.GenerateSetupAsync(userId, cancellationToken);
        return Ok(new { secret, provisioningUri, qr = qrBase64 });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest req, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var ok = await _mfaService.VerifyAsync(userId, req.Code, cancellationToken);
        if (!ok) return BadRequest(new { message = "Invalid code" });
        return Ok();
    }

    [HttpGet("recovery-codes")]
    public async Task<IActionResult> RecoveryCodes(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var codes = await _mfaService.GenerateRecoveryCodesAsync(userId, 8, cancellationToken);
        return Ok(new { recoveryCodes = codes });
    }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var ok = await _mfaService.DisableAsync(userId, cancellationToken);
        if (!ok) return BadRequest();
        return Ok();
    }

    public sealed class VerifyRequest
    {
        public string Code { get; set; } = string.Empty;
    }
}
