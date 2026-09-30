using Asp.Versioning.Builder;
using Core.Application.Cashflow;
using Core.Auth;
using Core.Domain;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class CashflowEndpoints
{
    public static IEndpointRouteBuilder MapCashflowEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/cashflow")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Cashflow");

        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            int? month,
            int? year,
            ProjectCostType? type,
            ProjectCostCategory? category,
            Guid? projectId,
            Guid? departmentId,
            string? search,
            int? page,
            int? pageSize,
            bool includeFilterOptions = true) =>
        {
            var result = await mediator.Send(
                new GetCashflowEntriesQuery(
                    new CashflowListFilters(
                        month,
                        year,
                        type,
                        category,
                        projectId,
                        departmentId,
                        search,
                        page,
                        pageSize,
                        includeFilterOptions)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowRead))
        .WithName("GetCashflowEntries");

        group.MapPost("/", async (
            CreateCashflowEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateCashflowEntryCommand(request), cancellationToken);
            return result.ToCreatedResult(response => $"/api/v1/cashflow/{response.PrimaryEntry.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowWrite))
        .WithName("CreateCashflowEntry");

        group.MapGet("/report", async (
            int month,
            int year,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetCashflowReportQuery(new CashflowReportFilters(month, year)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowRead))
        .WithName("GetCashflowReport");

        group.MapGet("/installments/{compraId:guid}", async (
            Guid compraId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCashflowInstallmentGroupQuery(compraId), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowRead))
        .WithName("GetCashflowInstallmentGroup");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCashflowEntryByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowRead))
        .WithName("GetCashflowEntryById");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCashflowEntryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateCashflowEntryCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowWrite))
        .WithName("UpdateCashflowEntry");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteCashflowEntryCommand(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CashflowWrite))
        .WithName("DeleteCashflowEntry");

        return app;
    }
}
