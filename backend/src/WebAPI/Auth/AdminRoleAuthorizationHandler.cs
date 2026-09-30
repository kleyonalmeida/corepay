using Core.Auth;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class AdminRoleAuthorizationHandler
    : AuthorizationHandler<AdminRoleRequirement>
{
    private readonly ICurrentUserAuthorizationStateProvider _authorizationStateProvider;
    private readonly ILogger<AdminRoleAuthorizationHandler> _logger;

    public AdminRoleAuthorizationHandler(
        ICurrentUserAuthorizationStateProvider authorizationStateProvider,
        ILogger<AdminRoleAuthorizationHandler> logger)
    {
        _authorizationStateProvider = authorizationStateProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRoleRequirement requirement)
    {
        var state = await _authorizationStateProvider.GetAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (state.IsInRole(AppRoles.Admin) || state.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Admin role policy denied for user {UserId}.",
            state.UserId);
    }
}

public sealed class SuperAdminRoleAuthorizationHandler
    : AuthorizationHandler<SuperAdminRoleRequirement>
{
    private readonly ICurrentUserAuthorizationStateProvider _authorizationStateProvider;
    private readonly ILogger<SuperAdminRoleAuthorizationHandler> _logger;

    public SuperAdminRoleAuthorizationHandler(
        ICurrentUserAuthorizationStateProvider authorizationStateProvider,
        ILogger<SuperAdminRoleAuthorizationHandler> logger)
    {
        _authorizationStateProvider = authorizationStateProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRoleRequirement requirement)
    {
        var state = await _authorizationStateProvider.GetAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (state.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "SuperAdmin role policy denied for user {UserId}.",
            state.UserId);
    }
}
