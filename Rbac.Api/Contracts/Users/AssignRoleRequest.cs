using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Users;

public record AssignRoleRequest([Required] Guid RoleId);
