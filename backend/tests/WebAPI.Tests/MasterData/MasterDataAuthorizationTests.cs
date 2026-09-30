using System.Net;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.MasterData;

[Collection("WebApiIntegration")]
public class MasterDataAuthorizationTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public MasterDataAuthorizationTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/departments")]
    [InlineData("/api/v1/career-levels")]
    [InlineData("/api/v1/projects")]
    [InlineData("/api/v1/payment-methods")]
    [InlineData("/api/v1/collaborators")]
    public async Task GetMasterData_WithoutToken_ShouldReturnUnauthorized(string route)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPaymentMethods_WithManagerToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/payment-methods");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/departments")]
    [InlineData("/api/v1/career-levels")]
    [InlineData("/api/v1/projects")]
    [InlineData("/api/v1/payment-methods")]
    [InlineData("/api/v1/collaborators")]
    public async Task GetMasterData_WithUserToken_ShouldReturnForbidden(string route)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDepartments_WithAdminToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/departments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
