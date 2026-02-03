using Microsoft.AspNetCore.Authorization;

namespace Rbac.Api.Application.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
