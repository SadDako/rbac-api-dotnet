using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Roles;

public record UpdateRoleRequest([Required] string Name);
