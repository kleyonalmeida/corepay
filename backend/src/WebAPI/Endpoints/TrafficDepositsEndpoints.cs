using Asp.Versioning.Builder;
using Core.Application.Traffic;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class TrafficDepositsEndpoints
{
    public static IEndpointRouteBuilder MapTrafficDepositsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/traffic-deposits")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("TrafficDeposits");

        group.MapGet("/", async (
            int? month,
            int? year,
            Guid? projectId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetTrafficProjectDepositsQuery(new TrafficProjectDepositListFilters(month, year, projectId)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficRead))
        .WithName("GetTrafficDeposits");

        group.MapPost("/", async (
            CreateTrafficProjectDepositRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateTrafficProjectDepositCommand(request), cancellationToken);
            return result.ToCreatedResult(deposit => $"/api/v1/traffic-deposits/{deposit.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.TrafficWrite))
        .WithName("CreateTrafficDeposit");

        return app;
    }
}
