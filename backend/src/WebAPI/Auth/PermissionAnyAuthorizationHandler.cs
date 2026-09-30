using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class PermissionAnyAuthorizationHandler
    : AuthorizationHandler<PermissionAnyRequirement>
{
    private readonly ICurrentUserAuthorizationStateProvider _authorizationStateProvider;
    private readonly ILogger<PermissionAnyAuthorizationHandler> _logger;

    public PermissionAnyAuthorizationHandler(
        ICurrentUserAuthorizationStateProvider authorizationStateProvider,
        ILogger<PermissionAnyAuthorizationHandler> logger)
    {
        _authorizationStateProvider = authorizationStateProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAnyRequirement requirement)
    {
        var state = await _authorizationStateProvider.GetAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (requirement.Permissions.Any(state.HasPermission))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "Permission-any policy denied for user {UserId}.",
            state.UserId);
    }
}
