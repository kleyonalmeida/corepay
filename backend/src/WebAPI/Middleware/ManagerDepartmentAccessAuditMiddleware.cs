using Core.Auth;
using WebAPI.Auth;

namespace WebAPI.Middleware;

public sealed class ManagerDepartmentAccessAuditMiddleware
{
    private const string DepartmentForbiddenMarker = "department_forbidden";

    private readonly RequestDelegate _next;
    private readonly ILogger<ManagerDepartmentAccessAuditMiddleware> _logger;

    public ManagerDepartmentAccessAuditMiddleware(
        RequestDelegate next,
        ILogger<ManagerDepartmentAccessAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserAuthorizationStateProvider authorizationStateProvider)
    {
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                buffer.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(buffer, leaveOpen: true);
                var responseBody = await reader.ReadToEndAsync();

                if (responseBody.Contains(DepartmentForbiddenMarker, StringComparison.Ordinal))
                {
                    var state = await authorizationStateProvider.GetAsync(context.User, context.RequestAborted);
                    if (state is not null && state.IsInRole(AppRoles.Manager))
                    {
                        _logger.LogWarning(
                            "Manager department access blocked. UserId={UserId} Method={Method} Route={Route}",
                            state.UserId,
                            context.Request.Method,
                            context.Request.Path.Value);
                    }
                }
            }
        }
        finally
        {
            buffer.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBody;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
    }
}
