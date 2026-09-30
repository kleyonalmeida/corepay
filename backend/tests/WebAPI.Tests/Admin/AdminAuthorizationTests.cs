using System.Net;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Admin;

[Collection("WebApiIntegration")]
public class AdminAuthorizationTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public AdminAuthorizationTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoles_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRoles_WithManagerToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoles_WithSuperAdminToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(
            client,
            CorePayWebApplicationFactory.SuperAdminEmail,
            CorePayWebApplicationFactory.SuperAdminPassword);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoles_WithAdminToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
