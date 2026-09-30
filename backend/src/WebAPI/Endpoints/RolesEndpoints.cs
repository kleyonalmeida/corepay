using Asp.Versioning.Builder;
using Core.Application.Admin;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/roles")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Roles");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetRolesQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.RolesRead))
        .WithName("GetRoles");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetRoleByIdQuery(id.ToString()), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.RolesRead))
        .WithName("GetRoleById");

        group.MapPost("/", async (
            CreateRoleRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateRoleCommand(request), cancellationToken);
            return result.ToCreatedResult(role => $"/api/v1/roles/{role.Id}");
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.SuperAdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.RolesWrite))
        .WithName("CreateRole");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRoleRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateRoleCommand(id.ToString(), request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.SuperAdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.RolesWrite))
        .WithName("UpdateRole");

        return app;
    }
}
