using System.Security.Claims;
using Asp.Versioning.Builder;
using Core.Application.Reports;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/reports")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Reports");

        group.MapGet("/payroll", async (
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
                new GetPayrollReportQuery(
                    new PayrollReportFilters(year, departmentId, projectId),
                    access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.ReportsRead))
        .WithName("GetPayrollReport");

        group.MapGet("/payroll/export", async (
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
                new GetPayrollReportExportQuery(
                    new PayrollReportFilters(year, departmentId, projectId),
                    access),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToHttpResult();
            }

            var export = result.Value!;
            return Results.File(export.Content, export.ContentType, export.FileName);
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.ReportsRead))
        .WithName("ExportPayrollReport");

        return app;
    }
}
