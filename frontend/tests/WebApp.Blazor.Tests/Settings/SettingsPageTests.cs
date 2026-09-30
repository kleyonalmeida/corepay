using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using SettingsPage = WebApp.Blazor.Pages.Settings;

namespace WebApp.Blazor.Tests.SettingsUi;

public class SettingsPageTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public SettingsPageTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
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
        Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
        Services.AddScoped<ICareerLevelApiService, CareerLevelApiService>();
        Services.AddScoped<IProjectApiService, ProjectApiService>();
        Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
    }

    [Fact]
    public async Task SettingsPage_Admin_RendersAllSectionsWithPageChrome()
    {
        ConfigureEmptyListResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.Admin],
            ReferenceRolePermissions.Map["Admin"]);

        var cut = Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SettingsPage>());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("text-page-title");
            cut.Markup.Should().Contain("Configurações");
            cut.Markup.Should().Contain("settings-page");
            cut.Markup.Should().Contain("Setores");
            cut.Markup.Should().Contain("Níveis de carreira");
            cut.Markup.Should().Contain("Projetos");
            cut.Markup.Should().Contain("Formas de pagamento");
        });
    }

    private void ConfigureEmptyListResponses()
    {
        _httpHandler.Configure(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path is "/api/v1/departments"
                or "/api/v1/career-levels"
                or "/api/v1/projects"
                or "/api/v1/payment-methods")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
