using Microsoft.AspNetCore.Authorization;

namespace WebApp.Blazor.Auth;

public sealed class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.Permissions.Any(permission =>
                context.User.Claims.Any(c =>
                    c.Type == "permission"
                    && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase))))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
