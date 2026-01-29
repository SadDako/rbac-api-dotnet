namespace Rbac.Api.Contracts.Auth;

public record AuthResponse(string Token, string Email, string Name, IEnumerable<string> Roles); 