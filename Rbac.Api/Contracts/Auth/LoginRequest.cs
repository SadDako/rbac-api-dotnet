using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Auth;

public record LoginRequest(string Email, string Password);