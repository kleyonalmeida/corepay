using System.Net;
using Core.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Security;

[Collection("WebApiIntegration")]
public sealed class ProtectedRoutesAuthorizationTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public ProtectedRoutesAuthorizationTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static readonly Guid AnyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static IReadOnlyList<ProtectedRouteCase> ProtectedRoutes =>
    [
        new(HttpMethod.Get, "/api/v1/users", AppPermissions.UsersRead, "Admin", HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/roles", AppPermissions.RolesRead, "Admin", HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/permissions", AppPermissions.PermissionsRead, "Admin", HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/departments", AppPermissions.DepartmentsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/career-levels", AppPermissions.CareerLevelsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/projects", AppPermissions.ProjectsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/payment-methods", AppPermissions.PaymentMethodsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/collaborators", AppPermissions.CollaboratorsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/payrolls", AppPermissions.PayrollsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Post, $"/api/v1/payrolls/{AnyId}/submit", AppPermissions.PayrollsWrite, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Post, $"/api/v1/payrolls/{AnyId}/approve", AppPermissions.PayrollsApprove, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Post, $"/api/v1/payrolls/{AnyId}/pay", AppPermissions.PayrollsPay, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Delete, $"/api/v1/payrolls/{AnyId}", AppPermissions.PayrollsDelete, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/finance/summary", AppPermissions.FinanceRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/project-revenues", AppPermissions.RevenuesRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/analyst-metrics", AppPermissions.AnalystMetricsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/reports/payroll?year=2026", AppPermissions.ReportsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/reports/payroll/export?year=2026", AppPermissions.ReportsRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/cashflow", AppPermissions.CashflowRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/traffic-investments", AppPermissions.TrafficRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/traffic-deposits", AppPermissions.TrafficRead, null, HttpStatusCode.Forbidden),
        new(HttpMethod.Get, "/api/v1/notifications", null, "Authenticated", HttpStatusCode.OK),
        new(HttpMethod.Get, "/api/v1/dashboard", null, "Authenticated", HttpStatusCode.OK),
        new(HttpMethod.Get, "/api/v1/auth/me", null, "Authenticated", HttpStatusCode.OK)
    ];

    [Fact]
    public async Task ProtectedRoutes_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        foreach (var route in ProtectedRoutes)
        {
            var response = await client.SendAsync(new HttpRequestMessage(route.Method, route.Url));
            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized,
                $"{route.Method} {route.Url} must require authentication");
        }
    }

    [Fact]
    public async Task ProtectedRoutes_WithUserWithoutPermissions_ShouldMatchAuthorizationMatrix()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        foreach (var route in ProtectedRoutes)
        {
            var response = await client.SendAsync(new HttpRequestMessage(route.Method, route.Url));
            response.StatusCode.Should().Be(
                route.StatusWithoutPermission,
                $"{route.Method} {route.Url}; permission={route.Permission}; role={route.Role}");
        }
    }

    [Fact]
    public void EveryApiEndpoint_ShouldDeclareAuthorizationOrApprovedAnonymousAccess()
    {
        using var scope = _factory.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .ToList();

        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText!;
            var hasAuthorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;
            var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var approvedAnonymous = route.Contains("/health", StringComparison.Ordinal)
                                    || route.Contains("/auth/login", StringComparison.Ordinal)
                                    || route.Contains("/webhooks/facilities/cashflow", StringComparison.Ordinal);

            (hasAuthorization || (allowsAnonymous && approvedAnonymous)).Should().BeTrue(
                $"endpoint {route} must be protected or be an approved anonymous endpoint");
            if (allowsAnonymous)
            {
                approvedAnonymous.Should().BeTrue(
                    $"endpoint {route} is not approved for anonymous access");
            }
        }
    }

    [Fact]
    public async Task HealthRoute_ShouldAllowAnonymous()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public sealed record ProtectedRouteCase(
        HttpMethod Method,
        string Url,
        string? Permission,
        string? Role,
        HttpStatusCode StatusWithoutPermission);
}
