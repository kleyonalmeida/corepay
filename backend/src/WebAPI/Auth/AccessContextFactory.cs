using Core.Application.AnalystMetrics;
using Core.Application.Collaborators;
using Core.Application.Notifications;
using Core.Application.Payrolls;
using System.Security.Claims;

namespace WebAPI.Auth;

public static class AccessContextFactory
{
    public static async Task<PayrollAccessContext> CreatePayrollAccessContextAsync(
        ClaimsPrincipal user,
        ICurrentUserAuthorizationStateProvider provider,
        CancellationToken cancellationToken)
    {
        var state = await provider.GetAsync(user, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user state is unavailable.");

        IReadOnlyList<Guid>? allowedDepartmentIds = null;
        if (CollaboratorAccessResolver.IsDepartmentScoped(state.Roles))
        {
            allowedDepartmentIds = state.DepartmentIds;
        }

        return new PayrollAccessContext(
            state.UserId,
            state.Roles,
            allowedDepartmentIds,
            state.DisplayName,
            state.Permissions);
    }

    public static async Task<CollaboratorAccessContext> CreateCollaboratorAccessContextAsync(
        ClaimsPrincipal user,
        ICurrentUserAuthorizationStateProvider provider,
        CancellationToken cancellationToken)
    {
        var state = await provider.GetAsync(user, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user state is unavailable.");

        IReadOnlyList<Guid>? allowedDepartmentIds = null;
        if (CollaboratorAccessResolver.IsDepartmentScoped(state.Roles))
        {
            allowedDepartmentIds = state.DepartmentIds;
        }

        return new CollaboratorAccessContext(state.UserId, state.Roles, allowedDepartmentIds);
    }

    public static async Task<AnalystMetricAccessContext> CreateAnalystMetricAccessContextAsync(
        ClaimsPrincipal user,
        ICurrentUserAuthorizationStateProvider provider,
        CancellationToken cancellationToken)
    {
        var state = await provider.GetAsync(user, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user state is unavailable.");

        IReadOnlyList<Guid>? allowedDepartmentIds = null;
        if (CollaboratorAccessResolver.IsDepartmentScoped(state.Roles))
        {
            allowedDepartmentIds = state.DepartmentIds;
        }

        return new AnalystMetricAccessContext(state.Roles, allowedDepartmentIds);
    }

    public static async Task<NotificationAccessContext> CreateNotificationAccessContextAsync(
        ClaimsPrincipal user,
        ICurrentUserAuthorizationStateProvider provider,
        CancellationToken cancellationToken)
    {
        var state = await provider.GetAsync(user, cancellationToken)
            ?? throw new InvalidOperationException("Authenticated user state is unavailable.");

        return new NotificationAccessContext(state.UserId, state.Roles);
    }
}
