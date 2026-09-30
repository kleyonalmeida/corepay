using Asp.Versioning.Builder;
using Core.Application.MasterData;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class PaymentMethodsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentMethodsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/payment-methods")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("PaymentMethods");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPaymentMethodsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PaymentMethodsRead))
        .WithName("GetPaymentMethods");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPaymentMethodByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PaymentMethodsRead))
        .WithName("GetPaymentMethodById");

        group.MapPost("/", async (
            CreatePaymentMethodRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreatePaymentMethodCommand(request), cancellationToken);
            return result.ToCreatedResult(method => $"/api/v1/payment-methods/{method.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PaymentMethodsWrite))
        .WithName("CreatePaymentMethod");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePaymentMethodRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdatePaymentMethodCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PaymentMethodsWrite))
        .WithName("UpdatePaymentMethod");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeletePaymentMethodCommand(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.PaymentMethodsWrite))
        .WithName("DeletePaymentMethod");

        return app;
    }
}
