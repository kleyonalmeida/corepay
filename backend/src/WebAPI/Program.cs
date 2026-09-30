using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Core;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using WebAPI.Auth;
using WebAPI.Configuration;
using WebAPI.Endpoints;
using WebAPI.Middleware;

EnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCorePayJwtAuthentication(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var isTesting = builder.Environment.IsEnvironment("Testing");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            NormalizeClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 10_000 : 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("webhook", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            NormalizeClientIp(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 10_000 : 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    if (!isTesting)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                NormalizeClientIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 600,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null or { Length: 0 })
{
    var corsOriginsCsv = builder.Configuration["Cors:AllowedOrigins"];
    corsOrigins = string.IsNullOrWhiteSpace(corsOriginsCsv)
        ? null
        : corsOriginsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

if (builder.Environment.IsProduction())
{
    if (corsOrigins is null or { Length: 0 })
    {
        throw new InvalidOperationException("Production requires Cors:AllowedOrigins with explicit HTTPS origins.");
    }

    if (corsOrigins.Any(origin =>
            origin.Contains('*', StringComparison.Ordinal)
            || origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("Production CORS origins cannot use wildcards or localhost.");
    }
}

corsOrigins ??=
[
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:5001",
    "https://localhost:5001"
];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }

    var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await identitySeeder.SeedAsync();

    var seedOptions = scope.ServiceProvider.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<SeedOptions>>().Value;
    if (app.Environment.IsDevelopment())
    {
        if (seedOptions.LoadFixtures)
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentFixtureSeeder>().SeedAsync();
        }

        if (seedOptions.LoadDemoData)
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentDemoDataSeeder>().SeedAsync();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["X-Frame-Options"] = "DENY";

    if (app.Environment.IsProduction())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    await next();
});
app.UseCors("BlazorClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<LiveUserValidationMiddleware>();
app.UseAuthorization();
app.UseMiddleware<PerformanceMetricsMiddleware>();
app.UseMiddleware<ManagerDepartmentAccessAuditMiddleware>();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapRolesEndpoints();
app.MapPermissionsEndpoints();
app.MapUsersEndpoints();
app.MapDepartmentsEndpoints();
app.MapCareerLevelsEndpoints();
app.MapProjectsEndpoints();
app.MapPaymentMethodsEndpoints();
app.MapCollaboratorsEndpoints();
app.MapPayrollsEndpoints();
app.MapFinanceEndpoints();
app.MapDashboardEndpoints();
app.MapReportsEndpoints();
app.MapProjectRevenuesEndpoints();
app.MapAnalystMetricsEndpoints();
app.MapCashflowEndpoints();
app.MapFacilitiesWebhookEndpoints();
app.MapTrafficInvestmentsEndpoints();
app.MapTrafficDepositsEndpoints();
app.MapNotificationsEndpoints();

app.Run();

static string NormalizeClientIp(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

public partial class Program;
