using Microsoft.AspNetCore.Mvc;
using Rbac.Api.Application.Auth;
using Rbac.Api.Contracts.Auth;
using Rbac.Api.Infrastructure.Http;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        try
        {
            return Ok(await _auth.RegisterAsync(req));
        }
        catch (InvalidOperationException)
        {
            return this.ToApiProblem(StatusCodes.Status409Conflict, "auth.email_already_exists", "This email is already registered.");
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        try
        {
            return Ok(await _auth.LoginAsync(req));
        }
        catch (InvalidOperationException)
        {
            return this.ToApiProblem(StatusCodes.Status401Unauthorized, "auth.invalid_credentials", "Invalid credentials.");
        }
    }
}
