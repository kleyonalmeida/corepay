using System.Net;
using System.Net.Http.Json;
using Core.Auth;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Admin;

[Collection("WebApiIntegration")]
public class PermissionsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PermissionsEndpointTests(CorePayWebApplicationFactory factory)
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
    public async Task GetPermissions_ShouldReturnAllSeedPermissions()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.GetAsync("/api/v1/permissions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissions = await response.Content.ReadFromJsonAsync<List<PermissionApiResponse>>();
        permissions.Should().NotBeNull();
        permissions!.Select(p => p.Key).Should().Contain(AppPermissions.All);
        permissions.Count.Should().BeGreaterThanOrEqualTo(AppPermissions.All.Count);
        permissions.Should().OnlyContain(p => p.Id != Guid.Empty);
    }

    [Fact]
    public async Task CreatePermission_ShouldPersistDynamicKey()
    {
        var client = await CreateSuperAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/permissions", new
        {
            key = "custom.feature",
            description = "Custom feature access"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PermissionApiResponse>();
        created!.Key.Should().Be("custom.feature");

        var getResponse = await client.GetAsync($"/api/v1/permissions/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePermission_ShouldChangeDescriptionOnly()
    {
        var client = await CreateSuperAdminClientAsync();
        var permissions = await client.GetFromJsonAsync<List<PermissionApiResponse>>("/api/v1/permissions");
        var target = permissions!.First(p => p.Key == AppPermissions.RolesRead);

        var response = await client.PutAsJsonAsync($"/api/v1/permissions/{target.Id}", new
        {
            description = "Read roles"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PermissionApiResponse>();
        updated!.Key.Should().Be(AppPermissions.RolesRead);
        updated.Description.Should().Be("Read roles");
    }

    private sealed record PermissionApiResponse(Guid Id, string Key, string Description);
}
