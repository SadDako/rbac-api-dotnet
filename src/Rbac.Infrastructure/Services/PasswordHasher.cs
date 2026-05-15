using Rbac.Application.Interfaces;

namespace Rbac.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return global::BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool Verify(string password, string passwordHash)
    {
        return global::BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
