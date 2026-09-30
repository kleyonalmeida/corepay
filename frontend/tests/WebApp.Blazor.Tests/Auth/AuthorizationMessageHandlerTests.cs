using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

public class AuthorizationMessageHandlerTests
{
    [Fact]
    public async Task Handler_AddsBearerHeader_WhenSessionPresent()
    {
        HttpRequestMessage? captured = null;
        using var ctx = new AuthTestContext(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var session = new AuthSession(
            "token-xyz",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse("id", "a@b.com", "User", [], []));

        ctx.StateProvider.SetSession(session);

        var response = await ctx.HttpClient.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "token-xyz"));
    }

    [Fact]
    public async Task Handler_OmitsBearerHeader_WhenAnonymous()
    {
        HttpRequestMessage? captured = null;
        using var ctx = new AuthTestContext(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var response = await ctx.HttpClient.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        captured.Should().NotBeNull();
        captured!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Handler_OnForbidden_RefreshesClaimsButPreservesForbidden()
    {
        using var ctx = new AuthTestContext(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/auth/me")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CurrentUserResponse(
                        "id",
                        "a@b.com",
                        "User",
                        ["User"],
                        [],
                        []))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });
        ctx.StateProvider.SetSession(new AuthSession(
            "token-xyz",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse("id", "a@b.com", "User", ["Admin"], ["users.write"])));

        var response = await ctx.HttpClient.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ctx.HttpHandler.RequestCount.Should().Be(2);
        ctx.StateProvider.CurrentSession!.User.Roles.Should().Equal("User");
        ctx.StateProvider.CurrentSession.User.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_OnMeForbidden_DoesNotRecurse()
    {
        using var ctx = new AuthTestContext(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        ctx.StateProvider.SetSession(new AuthSession(
            "token-xyz",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse("id", "a@b.com", "User", [], [])));

        var response = await ctx.HttpClient.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ctx.HttpHandler.RequestCount.Should().Be(1);
    }
}
