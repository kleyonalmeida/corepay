using System.Security.Claims;
using Core.Application.Finance;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/finance")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Finance");

        group.MapGet("/summary", async (
            int? month,
            int? year,
            Guid? departmentId,
            Guid? projectId,
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
                new GetFinanceSummaryQuery(
                    new FinanceSummaryFilters(month, year, departmentId, projectId),
                    access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.FinanceRead))
        .WithName("GetFinanceSummary");

        return app;
    }
}
