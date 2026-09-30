using Asp.Versioning.Builder;
using Core.Application.Admin;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class PermissionsEndpoints
{
    public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/permissions")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Permissions");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPermissionsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.PermissionsRead))
        .WithName("GetPermissions");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPermissionByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.PermissionsRead))
        .WithName("GetPermissionById");

        group.MapPost("/", async (
            CreatePermissionRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreatePermissionCommand(request), cancellationToken);
            return result.ToCreatedResult(permission => $"/api/v1/permissions/{permission.Id}");
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.SuperAdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.PermissionsWrite))
        .WithName("CreatePermission");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePermissionRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdatePermissionCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.SuperAdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.PermissionsWrite))
        .WithName("UpdatePermission");

        return app;
    }
}
