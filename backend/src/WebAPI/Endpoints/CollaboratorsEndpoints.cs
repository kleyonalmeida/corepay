using System.Security.Claims;
using Asp.Versioning.Builder;
using Core.Application.Collaborators;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class CollaboratorsEndpoints
{
    public static IEndpointRouteBuilder MapCollaboratorsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/collaborators")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Collaborators");

        group.MapGet("/", async (
            Guid? departmentId,
            string? search,
            bool? isActive,
            int? page,
            int? pageSize,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateCollaboratorAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(
                new GetCollaboratorsQuery(departmentId, search, isActive, access, page, pageSize),
                cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CollaboratorsRead))
        .WithName("GetCollaborators");

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateCollaboratorAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new GetCollaboratorByIdQuery(id, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CollaboratorsRead))
        .WithName("GetCollaboratorById");

        group.MapPost("/", async (
            CreateCollaboratorRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateCollaboratorAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new CreateCollaboratorCommand(request, access), cancellationToken);
            return result.ToCreatedResult(collaborator => $"/api/v1/collaborators/{collaborator.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CollaboratorsWrite))
        .WithName("CreateCollaborator");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCollaboratorRequest request,
            ClaimsPrincipal user,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var access = await AccessContextFactory.CreateCollaboratorAccessContextAsync(
                user,
                authorizationStateProvider,
                cancellationToken);
            var result = await mediator.Send(new UpdateCollaboratorCommand(id, request, access), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CollaboratorsWrite))
        .WithName("UpdateCollaborator");

        return app;
    }
}
