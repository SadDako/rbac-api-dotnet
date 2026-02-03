namespace Rbac.Api.Contracts.Roles;

public record RoleResponse(Guid Id, string Name, IReadOnlyCollection<string> Permissions);
