using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rbac.Api.Application.Interfaces;
using Rbac.Api.Contracts.Auth;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Options;

namespace Rbac.Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserStore _store;
    private readonly JwtOptions _jwt;

    public AuthService(IUserStore store, IOptions<JwtOptions> jwtOptions)
    {
        _store = store;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _store.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email já cadastrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Name = request.Name.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _store.CreateUserAsync(user);
        await _store.EnsureRoleExistsAsync("User");
        await _store.AddRoleToUserAsync(user.Id, "User");

        var roles = await _store.GetUserRolesAsync(user.Id);
        var token = GenerateToken(user, roles);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes <= 0 ? 120 : _jwt.ExpiresMinutes)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var user = await _store.FindByEmailAsync(request.Email.Trim());
        if (user is null) throw new UnauthorizedAccessException("Credenciais inválidas.");

        var ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!ok) throw new UnauthorizedAccessException("Credenciais inválidas.");

        var roles = await _store.GetUserRolesAsync(user.Id);
        var token = GenerateToken(user, roles);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes <= 0 ? 120 : _jwt.ExpiresMinutes)
        };
    }
    
    private string GenerateToken(User user, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name)
        };

        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes <= 0 ? 120 : _jwt.ExpiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
