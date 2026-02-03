using System.ComponentModel.DataAnnotations;

namespace Rbac.Api.Contracts.Roles;

public record UpdateRolePermissionsRequest([Required] IReadOnlyCollection<string> Permissions);
