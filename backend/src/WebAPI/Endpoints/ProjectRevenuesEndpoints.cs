using Asp.Versioning.Builder;
using Core.Application.Revenues;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class ProjectRevenuesEndpoints
{
    public static IEndpointRouteBuilder MapProjectRevenuesEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/project-revenues")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("ProjectRevenues");

        group.MapGet("/", async (
            int? month,
            int? year,
            Guid? projectId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetProjectRevenuesQuery(new ProjectRevenueListFilters(month, year, projectId)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.RevenuesRead))
        .WithName("GetProjectRevenues");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetProjectRevenueByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.RevenuesRead))
        .WithName("GetProjectRevenueById");

        group.MapPost("/", async (
            CreateProjectRevenueRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateProjectRevenueCommand(request), cancellationToken);
            return result.ToCreatedResult(revenue => $"/api/v1/project-revenues/{revenue.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.RevenuesWrite))
        .WithName("CreateProjectRevenue");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProjectRevenueRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateProjectRevenueCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.RevenuesWrite))
        .WithName("UpdateProjectRevenue");

        return app;
    }
}
