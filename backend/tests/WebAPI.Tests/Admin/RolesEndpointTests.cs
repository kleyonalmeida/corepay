using System.Net;
using System.Net.Http.Json;
using Core.Auth;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Admin;

[Collection("WebApiIntegration")]
public class RolesEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public RolesEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateSuperAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(
            client,
            CorePayWebApplicationFactory.SuperAdminEmail,
            CorePayWebApplicationFactory.SuperAdminPassword);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    [Fact]
    public async Task GetRoles_ShouldIncludeReferenceRolesWithPermissions()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleApiResponse>>();
        roles.Should().NotBeNull();
        roles!.Select(r => r.Name).Should().Contain(AppRoles.Manager);

        var manager = roles.Single(r => r.Name == AppRoles.Manager);
        manager.PermissionKeys.Should().BeEquivalentTo(RolePermissionMap.ReferenceRolePermissions[AppRoles.Manager]);
        Guid.TryParse(manager.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateRole_ShouldPersistDynamicRoleWithPermissions()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/roles", new
        {
            name = "Auditor",
            permissionKeys = new[] { AppPermissions.ReportsRead }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RoleApiResponse>();
        created.Should().NotBeNull();
        Guid.TryParse(created!.Id, out _).Should().BeTrue();
        created.Name.Should().Be("Auditor");
        created.PermissionKeys.Should().BeEquivalentTo([AppPermissions.ReportsRead]);

        var getResponse = await client.GetAsync($"/api/v1/roles/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRole_WithEmptyPermissions_ShouldReturnBadRequest()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/roles", new
        {
            name = "AuditorEmpty",
            permissionKeys = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("roles.permissions_required");
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ShouldReturnConflict()
    {
        var client = await CreateSuperAdminClientAsync();
        var payload = new { name = "AuditorDup", permissionKeys = new[] { AppPermissions.ReportsRead } };

        (await client.PostAsJsonAsync("/api/v1/roles", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/roles", payload)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateRole_RenamingSuperAdmin_ShouldReturnForbidden()
    {
        var client = await CreateSuperAdminClientAsync();
        var roles = await client.GetFromJsonAsync<List<RoleApiResponse>>("/api/v1/roles");
        var superAdmin = roles!.Single(r => r.Name == AppRoles.SuperAdmin);

        var response = await client.PutAsJsonAsync($"/api/v1/roles/{superAdmin.Id}", new
        {
            name = "NotSuperAdmin",
            permissionKeys = superAdmin.PermissionKeys
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRole_ChangingSuperAdminPermissions_ShouldReturnForbidden()
    {
        var client = await CreateSuperAdminClientAsync();
        var roles = await client.GetFromJsonAsync<List<RoleApiResponse>>("/api/v1/roles");
        var superAdmin = roles!.Single(r => r.Name == AppRoles.SuperAdmin);

        var response = await client.PutAsJsonAsync($"/api/v1/roles/{superAdmin.Id}", new
        {
            name = AppRoles.SuperAdmin,
            permissionKeys = new[] { AppPermissions.ReportsRead }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("roles.superadmin_immutable");
    }

    [Fact]
    public async Task GetRoleById_WithInvalidGuidRoute_ShouldReturnNotFound()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.GetAsync("/api/v1/roles/not-a-guid");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record RoleApiResponse(string Id, string Name, IReadOnlyList<string> PermissionKeys);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}
