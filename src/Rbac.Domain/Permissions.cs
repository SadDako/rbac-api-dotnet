namespace Rbac.Domain;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string UsersDelete = "users.delete";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string PermissionsManage = "permissions.manage";
    public const string AuditLogsRead = "auditlogs.read";
    public const string AuthRefresh = "auth.refresh";
    public const string AuthRevoke = "auth.revoke";
    public const string AuthLogin = "auth.login";
    public const string AuthRegister = "auth.register";
}
