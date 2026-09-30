using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserAuthorizationStateProvider _authorizationStateProvider;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        ICurrentUserAuthorizationStateProvider authorizationStateProvider,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _authorizationStateProvider = authorizationStateProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var state = await _authorizationStateProvider.GetAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (state.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Permission policy {Policy} denied for user {UserId}.",
            requirement.Permission,
            state.UserId);
    }
}
