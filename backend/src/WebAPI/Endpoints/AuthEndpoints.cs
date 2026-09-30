using Asp.Versioning.Builder;
using BuildingBlocks.Results;
using System.Security.Claims;
using WebAPI.Auth;

namespace WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            LoginService loginService,
            CancellationToken cancellationToken) =>
        {
            var result = await loginService.LoginAsync(request, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error!.Category switch
                {
                    ErrorCategory.Unauthorized => Results.Unauthorized(),
                    _ => Results.Problem(result.Error.Message)
                };
            }

            return Results.Ok(result.Value);
        })
        .WithName("Login")
        .AllowAnonymous()
        .RequireRateLimiting("login");

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            ICurrentUserAuthorizationStateProvider authorizationStateProvider,
            CancellationToken cancellationToken) =>
        {
            var state = await authorizationStateProvider.GetAsync(principal, cancellationToken);
            if (state is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new CurrentUserResponse(
                state.UserId,
                state.Email,
                state.DisplayName,
                state.Roles,
                state.Permissions,
                state.DepartmentIds));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser");

        return app;
    }
}
