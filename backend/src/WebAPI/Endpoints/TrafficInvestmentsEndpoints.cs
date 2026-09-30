using Asp.Versioning.Builder;
using Core.Application.Traffic;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class TrafficInvestmentsEndpoints
{
    public static IEndpointRouteBuilder MapTrafficInvestmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/traffic-investments")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("TrafficInvestments");

        group.MapGet("/", async (
            int? month,
            int? year,
            Guid? projectId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetTrafficInvestmentsQuery(new TrafficInvestmentListFilters(month, year, projectId)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficRead))
        .WithName("GetTrafficInvestments");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetTrafficInvestmentByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficRead))
        .WithName("GetTrafficInvestmentById");

        group.MapPost("/", async (
            CreateTrafficInvestmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateTrafficInvestmentCommand(request), cancellationToken);
            return result.ToCreatedResult(investment => $"/api/v1/traffic-investments/{investment.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficWrite))
        .WithName("CreateTrafficInvestment");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTrafficInvestmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateTrafficInvestmentCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficWrite))
        .WithName("UpdateTrafficInvestment");

        return app;
    }
}
