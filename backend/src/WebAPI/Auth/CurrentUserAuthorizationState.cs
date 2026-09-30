namespace WebAPI.Auth;

public sealed record CurrentUserAuthorizationState(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> DepartmentIds)
{
    public const string HttpContextItemKey = "corepay.auth.current-user-state";

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) =>
        IsInRole(Core.Auth.AppRoles.SuperAdmin)
        || Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool IsAdminRole() =>
        IsInRole(Core.Auth.AppRoles.Admin) || IsInRole(Core.Auth.AppRoles.SuperAdmin);
}
