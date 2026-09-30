using System.Diagnostics;
using Infrastructure.Diagnostics;

namespace WebAPI.Middleware;

public sealed class PerformanceMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RequestPerformanceMetrics metrics)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            context.Response.Headers["X-Query-Count"] = metrics.QueryCount.ToString();
            context.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await next(context);
    }
}
