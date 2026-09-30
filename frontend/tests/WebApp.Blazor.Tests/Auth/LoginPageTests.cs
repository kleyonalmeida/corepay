using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Pages;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

public class LoginPageTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler = new(_ =>
        new HttpResponseMessage(HttpStatusCode.Unauthorized));

    public LoginPageTests()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        Services.AddScoped<AuthorizationMessageHandler>();
        Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
            handler.InnerHandler = _httpHandler;
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };
        });
        Services.AddScoped<AuthService>();
    }

    [Fact]
    public void LoginPage_RendersEmailPasswordAndSubmit()
    {
        var cut = Render<Login>();

        cut.Find("input[type='email']").Should().NotBeNull();
        cut.Find("input.ui-input--password-field").Should().NotBeNull();
        cut.Find(".ui-input__password-toggle").Should().NotBeNull();
        cut.Find("button.auth-submit").TextContent.Should().Contain("Entrar");
        cut.Find(".text-auth-title").TextContent.Should().Contain("CorePay");
        cut.Find(".auth-page").Should().NotBeNull();
        cut.Find(".auth-page__icon-wrap").Should().NotBeNull();
        cut.Find(".auth-card").Should().NotBeNull();
    }

    [Fact]
    public async Task LoginPage_InvalidCredentials_ShowsErrorBanner()
    {
        var cut = Render<Login>();

        cut.Find("input[type='email']").Input("user@test.com");
        cut.Find("input.ui-input--password-field").Input("wrong");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".auth-error").TextContent.Should().Contain("E-mail ou senha inválidos.");
        });
    }

}
