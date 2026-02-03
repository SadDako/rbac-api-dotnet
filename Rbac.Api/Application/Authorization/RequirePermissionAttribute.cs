using Microsoft.AspNetCore.Authorization;

namespace Rbac.Api.Application.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission
    {
        get => Policy?.StartsWith(PolicyPrefix, StringComparison.Ordinal) == true
            ? Policy[PolicyPrefix.Length..]
            : string.Empty;
        set => Policy = $"{PolicyPrefix}{value}";
    }
}
