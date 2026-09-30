using System.Security.Claims;

namespace WebApp.Blazor.Auth;

public static class DepartmentScopeHelper
{
    public static bool IsDepartmentScoped(ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Manager)
        && !user.IsInRole(AppRoles.SuperAdmin)
        && !user.IsInRole(AppRoles.Admin)
        && !user.IsInRole(AppRoles.Director)
        && !user.IsInRole(AppRoles.Financial);
}
