using System.Security.Claims;
using Asp.Versioning.Builder;
using Core.Application.Admin;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Users");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetUsersQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.UsersRead))
        .WithName("GetUsers");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetUserByIdQuery(id.ToString()), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.UsersRead))
        .WithName("GetUserById");

        group.MapPost("/", async (
            CreateUserRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new CreateUserCommand(request, GetActingUserId(user)),
                cancellationToken);
            return result.ToCreatedResult(user => $"/api/v1/users/{user.Id}");
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.UsersWrite))
        .WithName("CreateUser");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new UpdateUserCommand(id.ToString(), request, GetActingUserId(user)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.UsersWrite))
        .WithName("UpdateUser");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new DeleteUserCommand(id.ToString(), GetActingUserId(user)),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            PermissionAuthorizationExtensions.Policy(AppPermissions.UsersWrite))
        .WithName("DeleteUser");

        return app;
    }

    private static string GetActingUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? string.Empty;
}
