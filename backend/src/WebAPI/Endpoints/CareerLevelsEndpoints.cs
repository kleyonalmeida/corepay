using Asp.Versioning.Builder;
using Core.Application.MasterData;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class CareerLevelsEndpoints
{
    public static IEndpointRouteBuilder MapCareerLevelsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/career-levels")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("CareerLevels");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCareerLevelsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CareerLevelsRead))
        .WithName("GetCareerLevels");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetCareerLevelByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CareerLevelsRead))
        .WithName("GetCareerLevelById");

        group.MapPost("/", async (
            CreateCareerLevelRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateCareerLevelCommand(request), cancellationToken);
            return result.ToCreatedResult(level => $"/api/v1/career-levels/{level.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CareerLevelsWrite))
        .WithName("CreateCareerLevel");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCareerLevelRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateCareerLevelCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.CareerLevelsWrite))
        .WithName("UpdateCareerLevel");

        return app;
    }
}
