using System.Security.Claims;
using Core.Application.Dashboard;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        app.MapGet("/api/v{version:apiVersion}/dashboard", async (
                int? month,
                int? year,
                ClaimsPrincipal user,
                ICurrentUserAuthorizationStateProvider authorizationStateProvider,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                    user,
                    authorizationStateProvider,
                    cancellationToken);
                var result = await mediator.Send(
                    new GetDashboardQuery(new DashboardFilters(month, year), access),
                    cancellationToken);
                return result.ToHttpResult();
            })
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Dashboard")
            .RequireAuthorization()
            .WithName("GetDashboard");

        return app;
    }
}
