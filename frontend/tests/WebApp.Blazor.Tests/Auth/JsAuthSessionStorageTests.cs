using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

public class JsAuthSessionStorageTests : BlazorComponentTestContext
{
    [Fact]
    public async Task JsAuthSessionStorage_RoundTripsValidSession()
    {
        var session = new AuthSession(
            "token-abc",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse(
                "user-id",
                "user@test.com",
                "User",
                ["Admin"],
                [AppPermissions.PayrollsRead]));

        var json = System.Text.Json.JsonSerializer.Serialize(session, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        JSInterop.Setup<string?>("corepayAuth.getSession").SetResult(json);
        JSInterop.SetupVoid("corepayAuth.setSession");
        JSInterop.SetupVoid("corepayAuth.clearSession").SetVoidResult();

        var jsRuntime = Services.GetRequiredService<IJSRuntime>();
        var storage = new JsAuthSessionStorage(jsRuntime);
        await storage.SetAsync(session);

        var restored = await storage.GetAsync();

        restored.Should().NotBeNull();
        restored!.AccessToken.Should().Be("token-abc");
        restored.User.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task JsAuthSessionStorage_ExpiredSession_ClearsStorage()
    {
        var session = new AuthSession(
            "token-abc",
            DateTime.UtcNow.AddMinutes(-1),
            new LoginUserResponse("id", "a@b.com", "User", [], []));

        var json = System.Text.Json.JsonSerializer.Serialize(session, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        JSInterop.Setup<string?>("corepayAuth.getSession").SetResult(json);
        JSInterop.SetupVoid("corepayAuth.clearSession").SetVoidResult();

        var jsRuntime = Services.GetRequiredService<IJSRuntime>();
        var storage = new JsAuthSessionStorage(jsRuntime);
        var restored = await storage.GetAsync();

        restored.Should().BeNull();
        JSInterop.VerifyInvoke("corepayAuth.clearSession");
    }
}
