using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Pages;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

public class AppAuthorizationTests : BlazorComponentTestContext
{
    public AppAuthorizationTests()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<AuthService>();
        Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IDashboardApiService, DashboardApiService>();
    }

    [Fact]
    public async Task Home_RequiresAuthentication()
    {
        var authService = Services.GetRequiredService<AuthService>();
        await authService.InitializeAsync();

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Olá,");
        });
    }

    [Fact]
    public async Task Home_AuthenticatedUser_RendersGreeting()
    {
        var storage = Services.GetRequiredService<IAuthSessionStorage>();
        var stateProvider = Services.GetRequiredService<CorePayAuthenticationStateProvider>();
        var session = new AuthSession(
            "token",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse(
                "user-id",
                "user@test.com",
                "Usuário Teste",
                ["Admin"],
                [AppPermissions.PayrollsRead]));

        await storage.SetAsync(session);
        stateProvider.SetSession(session);

        var cut = Render<Home>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Usuário Teste");
        });
    }
}
