using System.Security.Claims;
using Asp.Versioning.Builder;
using Core.Application.AnalystMetrics;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class AnalystMetricsEndpoints
{
    public static IEndpointRouteBuilder MapAnalystMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/analyst-metrics")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("AnalystMetrics");

        group.MapGet("/", async (
            int? month,
            int? year,
            Guid? departmentId,
            Guid? collaboratorId,
            Guid? projectId,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateAnalystMetricAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(
                new GetAnalystMetricsQuery(
                    new AnalystMetricListFilters(month, year, departmentId, collaboratorId, projectId),
                    access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.AnalystMetricsRead))
        .WithName("GetAnalystMetrics");

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateAnalystMetricAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new GetAnalystMetricByIdQuery(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.AnalystMetricsRead))
        .WithName("GetAnalystMetricById");

        group.MapPost("/", async (
            CreateAnalystMetricRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateAnalystMetricAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new CreateAnalystMetricCommand(request, access), cancellationToken);
            return result.ToCreatedResult(metric => $"/api/v1/analyst-metrics/{metric.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.AnalystMetricsWrite))
        .WithName("CreateAnalystMetric");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateAnalystMetricRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateAnalystMetricAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(
                new UpdateAnalystMetricCommand(id, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.AnalystMetricsWrite))
        .WithName("UpdateAnalystMetric");

        return app;
    }
}
