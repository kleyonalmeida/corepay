using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Admin;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.AdminUi;

public class RoleManagementTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public RoleManagementTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IRoleApiService, RoleApiService>();
    }

    [Fact]
    public void AccessCatalog_CoversEveryApplicationPermission()
    {
        RoleAccessCatalog.Items.Select(item => item.PermissionKey)
            .Should().BeEquivalentTo(AppPermissions.All);
        RoleAccessCatalog.Items.Should().OnlyContain(item =>
            !string.IsNullOrWhiteSpace(item.Area)
            && !string.IsNullOrWhiteSpace(item.Route)
            && !string.IsNullOrWhiteSpace(item.Description));
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsPermissionCatalog()
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/permissions");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"id":"7fa85f64-5717-4562-b3fc-2c963f66afa6","key":"reports.read","description":"Reports"}]""")
            };
        });

        var result = await Services.GetRequiredService<IRoleApiService>().GetPermissionsAsync();

        result.Status.Should().Be(RoleApiStatus.Success);
        result.Permissions.Should().ContainSingle(permission => permission.Key == AppPermissions.ReportsRead);
    }

    [Fact]
    public async Task CreateRoleAsync_SerializesSelectedPermissions()
    {
        string? body = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/roles");
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":"role-id","name":"Auditor","permissionKeys":["reports.read"]}""")
            };
        });

        var result = await Services.GetRequiredService<IRoleApiService>().CreateRoleAsync(
            new CreateRoleRequest("Auditor", [AppPermissions.ReportsRead]));

        result.Status.Should().Be(RoleApiStatus.Success);
        result.Role!.Name.Should().Be("Auditor");
        body.Should().Contain("\"permissionKeys\":[\"reports.read\"]");
    }

    [Fact]
    public async Task UpdateRoleAsync_Forbidden_MapsApiError()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                """{"error":"roles.superadmin_immutable","message":"Protected."}""")
        });

        var result = await Services.GetRequiredService<IRoleApiService>().UpdateRoleAsync(
            "role-id",
            new UpdateRoleRequest("SuperAdmin", [AppPermissions.RolesRead]));

        result.Status.Should().Be(RoleApiStatus.Forbidden);
        result.ErrorCode.Should().Be("roles.superadmin_immutable");
    }
}
