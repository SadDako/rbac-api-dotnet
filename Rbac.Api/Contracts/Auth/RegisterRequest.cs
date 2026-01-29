using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Auth;

public record RegisterRequest(string Name, string Email, string Password);