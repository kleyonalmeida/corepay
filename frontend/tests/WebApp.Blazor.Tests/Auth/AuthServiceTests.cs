using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

public class AuthServiceTests
{
    private static LoginUserResponse CreateUser() => new(
        "11111111-1111-1111-1111-111111111111",
        "admin@corepay.test",
        "Admin User",
        ["Admin"],
        [AppPermissions.PayrollsRead]);

    [Fact]
    public async Task LoginAsync_ValidCredentials_StoresSessionAndAuthenticates()
    {
        using var ctx = new AuthTestContext(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/auth/login");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new LoginResponse(
                    "token-abc",
                    DateTime.UtcNow.AddHours(1),
                    CreateUser()))
            };
        });

        var result = await ctx.AuthService.LoginAsync("admin@corepay.test", "TestPassword123!");

        result.Succeeded.Should().BeTrue();
        ctx.AuthService.IsAuthenticated.Should().BeTrue();
        ctx.AuthService.CurrentSession!.AccessToken.Should().Be("token-abc");

        var stored = await ctx.SessionStorage.GetAsync();
        stored.Should().NotBeNull();
        stored!.User.Email.Should().Be("admin@corepay.test");
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsFailureWithoutSession()
    {
        using var ctx = new AuthTestContext(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await ctx.AuthService.LoginAsync("admin@corepay.test", "wrong");

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("E-mail ou senha inválidos.");
        ctx.AuthService.IsAuthenticated.Should().BeFalse();
        (await ctx.SessionStorage.GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_EmptyFields_ReturnsFailureWithoutCallingApi()
    {
        using var ctx = new AuthTestContext();

        var result = await ctx.AuthService.LoginAsync("", "password");

        result.Succeeded.Should().BeFalse();
        ctx.HttpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task LogoutAsync_ClearsSession()
    {
        using var ctx = new AuthTestContext();
        var session = new AuthSession(
            "token-abc",
            DateTime.UtcNow.AddHours(1),
            CreateUser());

        await ctx.SessionStorage.SetAsync(session);
        ctx.StateProvider.SetSession(session);

        await ctx.AuthService.LogoutAsync();

        ctx.AuthService.IsAuthenticated.Should().BeFalse();
        (await ctx.SessionStorage.GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_RestoresStoredSession()
    {
        using var ctx = new AuthTestContext(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/auth/me");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CurrentUserResponse(
                    "11111111-1111-1111-1111-111111111111",
                    "admin@corepay.test",
                    "Admin Atual",
                    ["Manager"],
                    ["reports.read"],
                    []))
            };
        });
        var session = new AuthSession(
            "token-abc",
            DateTime.UtcNow.AddHours(1),
            CreateUser());

        await ctx.SessionStorage.SetAsync(session);

        await ctx.AuthService.InitializeAsync();

        ctx.AuthService.IsAuthenticated.Should().BeTrue();
        ctx.AuthService.CurrentSession!.AccessToken.Should().Be("token-abc");
        ctx.AuthService.CurrentSession.User.DisplayName.Should().Be("Admin Atual");
        ctx.AuthService.CurrentSession.User.Roles.Should().Equal("Manager");
        ctx.AuthService.CurrentSession.User.Permissions.Should().Equal("reports.read");
    }

    [Fact]
    public async Task InitializeAsync_ExpiredStoredSession_DoesNotAuthenticate()
    {
        using var ctx = new AuthTestContext();
        var session = new AuthSession(
            "token-abc",
            DateTime.UtcNow.AddMinutes(-5),
            CreateUser());

        await ctx.SessionStorage.SetAsync(session);

        await ctx.AuthService.InitializeAsync();

        ctx.AuthService.IsAuthenticated.Should().BeFalse();
        (await ctx.SessionStorage.GetAsync()).Should().BeNull();
        ctx.HttpHandler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task InitializeAsync_WhenMeReturnsUnauthorized_ClearsStoredSession()
    {
        using var ctx = new AuthTestContext(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        await ctx.SessionStorage.SetAsync(new AuthSession(
            "revoked-token",
            DateTime.UtcNow.AddHours(1),
            CreateUser()));

        await ctx.AuthService.InitializeAsync();

        ctx.AuthService.IsAuthenticated.Should().BeFalse();
        (await ctx.SessionStorage.GetAsync()).Should().BeNull();
    }
}
