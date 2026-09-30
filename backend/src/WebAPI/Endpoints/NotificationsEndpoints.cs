using System.Security.Claims;
using Core.Application.Notifications;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapGet("/api/v{version:apiVersion}/notifications", async (
                ClaimsPrincipal user,
                int? page,
                int? pageSize,
                ICurrentUserAuthorizationStateProvider authorizationStateProvider,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var access = await AccessContextFactory.CreateNotificationAccessContextAsync(
                    user,
                    authorizationStateProvider,
                    cancellationToken);
                var result = await mediator.Send(
                    new GetNotificationsQuery(access, page, pageSize),
                    cancellationToken);
                return result.ToHttpResult();
            })
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Notifications")
            .RequireAuthorization()
            .WithName("GetNotifications");

        app.MapPut("/api/v{version:apiVersion}/notifications/{id:guid}/read", async (
                Guid id,
                ClaimsPrincipal user,
                ICurrentUserAuthorizationStateProvider authorizationStateProvider,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var access = await AccessContextFactory.CreateNotificationAccessContextAsync(
                    user,
                    authorizationStateProvider,
                    cancellationToken);
                var result = await mediator.Send(new MarkNotificationReadCommand(id, access), cancellationToken);
                return result.ToHttpResult();
            })
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Notifications")
            .RequireAuthorization()
            .WithName("MarkNotificationRead");

        return app;
    }
}
