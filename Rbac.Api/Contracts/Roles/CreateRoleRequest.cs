using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Roles;

public record CreateRoleRequest([Required] string Name);
