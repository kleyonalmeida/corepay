using System.Security.Claims;
using Core.Application.Payrolls;
using Core.Auth;
using Core.Domain;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class PayrollsEndpoints
{
    public static IEndpointRouteBuilder MapPayrollsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/payrolls")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Payrolls");

        group.MapGet("/form-options", async (
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new GetPayrollFormOptionsQuery(access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsRead))
        .WithName("GetPayrollFormOptions");

        group.MapGet("/", async (
            string? search,
            int? month,
            int? year,
            string? status,
            Guid? departmentId,
            int? page,
            int? pageSize,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            PayrollStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<PayrollStatus>(status, ignoreCase: true, out var statusValue))
                {
                    return Results.BadRequest(new { error = "payrolls.invalid_status", message = "Invalid payroll status." });
                }

                parsedStatus = statusValue;
            }

            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(
                new GetPayrollsQuery(search, month, year, parsedStatus, departmentId, access, page, pageSize),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsRead))
        .WithName("GetPayrolls");

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new GetPayrollByIdQuery(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsRead))
        .WithName("GetPayrollById");

        group.MapPost("/", async (
            CreatePayrollRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new CreatePayrollCommand(request, access), cancellationToken);
            return result.ToCreatedResult(payroll => $"/api/v1/payrolls/{payroll.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("CreatePayroll");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePayrollRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new UpdatePayrollCommand(id, request, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("UpdatePayroll");

        group.MapPost("/{id:guid}/submit", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new SubmitPayrollCommand(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("SubmitPayroll");

        group.MapPost("/{id:guid}/entries/{entryId:guid}/preview", async (
            Guid id,
            Guid entryId,
            PreviewPayrollEntryRequest request,
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
                new PreviewPayrollEntryCommand(id, entryId, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("PreviewPayrollEntry");

        group.MapPost("/{id:guid}/duplicate", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new DuplicatePayrollCommand(id, access), cancellationToken);
            return result.ToCreatedResult(payroll => $"/api/v1/payrolls/{payroll.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("DuplicatePayroll");

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new ApprovePayrollCommand(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsApprove))
        .WithName("ApprovePayroll");

        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectPayrollRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new RejectPayrollCommand(id, request, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsApprove))
        .WithName("RejectPayroll");

        group.MapPost("/{id:guid}/recalculate", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new RecalculatePayrollCommand(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("RecalculatePayroll");

        group.MapPost("/{id:guid}/pay", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new PayPayrollCommand(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsPay))
        .WithName("PayPayroll");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new DeletePayrollCommand(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsDelete))
        .WithName("DeletePayroll");

        group.MapPost("/{id:guid}/entries", async (
            Guid id,
            AddCollaboratorEntryRequest request,
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
                new AddCollaboratorEntryCommand(id, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsWrite))
        .WithName("AddCollaboratorEntry");

        group.MapPost("/{id:guid}/entries/{entryId:guid}/approve", async (
            Guid id,
            Guid entryId,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreatePayrollAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new ApprovePayrollEntryCommand(id, entryId, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsApprove))
        .WithName("ApprovePayrollEntry");

        group.MapPost("/{id:guid}/entries/{entryId:guid}/pay", async (
            Guid id,
            Guid entryId,
            SetEntryPaidRequest request,
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
                new PayPayrollEntryCommand(id, entryId, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsPay))
        .WithName("PayPayrollEntry");

        group.MapPut("/{id:guid}/entries/{entryId:guid}/nf", async (
            Guid id,
            Guid entryId,
            SetEntryNfRequest request,
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
                new SetPayrollEntryNfCommand(id, entryId, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PayrollsPay))
        .WithName("SetPayrollEntryNf");

        group.MapPut("/{id:guid}/entries/{entryId:guid}", async (
            Guid id,
            Guid entryId,
            UpdatePayrollEntryRequest request,
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
                new UpdatePayrollEntryCommand(id, entryId, request, access),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.AnyPolicy(
            AppPermissions.PayrollsWrite,
            AppPermissions.PayrollsPay))
        .WithName("UpdatePayrollEntry");

        return app;
    }
}
