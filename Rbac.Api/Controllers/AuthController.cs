using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Contracts.Auth;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Options;
using Rbac.Api.Services;

namespace Rbac.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private const string DefaultRoleName = "User";
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userExists = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (userExists)
        {
            return Conflict(new { message = "Email já cadastrado." });
        }

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == DefaultRoleName, cancellationToken);

        if (role is null)
        {
            role = new Role { Name = DefaultRoleName };
            _dbContext.Roles.Add(role);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        user.UserRoles.Add(new UserRole { Role = role, User = user });

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Created(string.Empty, new { user.Id, user.Email, user.Name });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        var authResponse = _tokenService.CreateAccessToken(user);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var (refreshTokenEntity, refreshTokenValue) = await _tokenService.CreateRefreshTokenAsync(user, ipAddress);

        authResponse.RefreshToken = refreshTokenValue;
        authResponse.RefreshTokenExpiresAtUtc = refreshTokenEntity.ExpiresAtUtc;
        return Ok(authResponse);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            var response = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken, ipAddress, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "Refresh token inválido ou expirado." });
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var revoked = await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, ipAddress, cancellationToken);
        if (!revoked)
        {
            return NotFound(new { message = "Refresh token não encontrado ou já revogado." });
        }

        return NoContent();
    }
}
