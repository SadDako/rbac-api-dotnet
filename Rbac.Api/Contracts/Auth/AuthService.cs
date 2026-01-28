using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rbac.Api.Contracts.Auth;
using Rbac.Api.Domain.Entities;
using Rbac.Api.Infrastructure.Data;
using Rbac.Api.Options;

namespace Rbac.Api.Application.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        var role = await _db.Roles.FirstAsync(r => r.Name == "User");

        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _db.SaveChangesAsync();

        var roles = new[] { role.Name };
        var token = CreateToken(user, roles);

        return new AuthResponse(token, user.Email, user.Name, roles);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            throw new InvalidOperationException("Credenciais inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new InvalidOperationException("Credenciais inválidas.");

        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToArray();

        var token = CreateToken(user, roles);

        return new AuthResponse(token, user.Email, user.Name, roles);
    }

    private string CreateToken(User user, string[] roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Key)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        claims.AddRange(
            roles.Select(r => new Claim(ClaimTypes.Role, r))
        );

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
