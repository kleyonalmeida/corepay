using Core.Auth;

namespace Core.Application.Collaborators;

public static class CollaboratorAccessResolver
{
    public static bool IsDepartmentScoped(IReadOnlyList<string> roles) =>
        roles.Contains(AppRoles.Manager, StringComparer.OrdinalIgnoreCase)
        && !roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase)
        && !roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
        && !roles.Contains(AppRoles.Director, StringComparer.OrdinalIgnoreCase)
        && !roles.Contains(AppRoles.Financial, StringComparer.OrdinalIgnoreCase);
}
