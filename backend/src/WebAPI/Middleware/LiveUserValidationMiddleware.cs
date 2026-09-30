using WebAPI.Auth;

namespace WebAPI.Middleware;

public sealed class LiveUserValidationMiddleware
{
    private readonly RequestDelegate _next;

    public LiveUserValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserAuthorizationStateProvider authorizationStateProvider)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var state = await authorizationStateProvider.GetAsync(context.User, context.RequestAborted);
            if (state is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }
}
