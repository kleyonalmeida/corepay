using Asp.Versioning.Builder;
using Core.Application.MasterData;
using Core.Auth;
using MediatR;
using WebAPI.Auth;
using WebAPI.Extensions;

namespace WebAPI.Endpoints;

public static class DepartmentsEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/departments")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Departments");

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetDepartmentsQuery(), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.DepartmentsRead))
        .WithName("GetDepartments");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetDepartmentByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.DepartmentsRead))
        .WithName("GetDepartmentById");

        group.MapPost("/", async (
            CreateDepartmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateDepartmentCommand(request), cancellationToken);
            return result.ToCreatedResult(department => $"/api/v1/departments/{department.Id}");
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.DepartmentsWrite))
        .WithName("CreateDepartment");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDepartmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateDepartmentCommand(id, request), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(PermissionAuthorizationExtensions.Policy(AppPermissions.DepartmentsWrite))
        .WithName("UpdateDepartment");

        return app;
    }
}
