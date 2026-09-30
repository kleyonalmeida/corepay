using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Core.Auth;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Auth;

[Collection("WebApiIntegration")]
public class LoginEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(CorePayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidSuperAdminCredentials_ShouldReturnUsableJwt()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = CorePayWebApplicationFactory.SuperAdminEmail,
            password = CorePayWebApplicationFactory.SuperAdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be(CorePayWebApplicationFactory.SuperAdminEmail);
        body.User.DisplayName.Should().NotBeNullOrWhiteSpace();
        body.User.Roles.Should().Contain(AppRoles.SuperAdmin);
        body.User.Permissions.Should().BeEquivalentTo(AppPermissions.All);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        jwt.Issuer.Should().Be("CorePay");
        jwt.Audiences.Should().Contain("CorePay");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
        jwt.Claims.Should().NotContain(c => c.Type == "permission");

        var authenticatedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        authenticatedRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);

        var healthResponse = await _client.SendAsync(authenticatedRequest);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("wrong@corepay.test", "TestPassword123!")]
    [InlineData("superadmin@corepay.test", "WrongPassword123!")]
    [InlineData("", "TestPassword123!")]
    [InlineData("superadmin@corepay.test", "")]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithCurrentToken_ShouldReturnPublicDatabaseAuthorizationState()
    {
        var token = await AuthTestHelper.LoginAsync(
            _client,
            CorePayWebApplicationFactory.SuperAdminEmail,
            CorePayWebApplicationFactory.SuperAdminPassword);
        AuthTestHelper.SetBearerToken(_client, token);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        root.GetProperty("email").GetString().Should().Be(CorePayWebApplicationFactory.SuperAdminEmail);
        root.GetProperty("roles").EnumerateArray().Select(item => item.GetString())
            .Should().Contain(AppRoles.SuperAdmin);
        root.TryGetProperty("accessToken", out _).Should().BeFalse();
        root.TryGetProperty("password", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginApiResponse(
        string AccessToken,
        DateTime ExpiresAtUtc,
        LoginUserApiResponse User);

    private sealed record LoginUserApiResponse(
        string Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);
}
