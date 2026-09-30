using Microsoft.AspNetCore.Authorization;

namespace WebApp.Blazor.Auth;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Claims.Any(c =>
                c.Type == "permission"
                && string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
