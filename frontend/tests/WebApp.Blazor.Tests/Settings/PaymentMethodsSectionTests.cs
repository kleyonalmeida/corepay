using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Settings;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Settings;

public class PaymentMethodsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public PaymentMethodsSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<AuthService>();
        Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
    }

    [Fact]
    public async Task PaymentMethodsSection_Admin_ShowsListAndCreateButton()
    {
        ConfigureApiResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

        var cut = Render<PaymentMethodsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nova forma");
            cut.Markup.Should().Contain("Pix");
        });
    }

    private void ConfigureApiResponses()
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath == "/api/v1/payment-methods")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        [
                          { "id": "8fa85f64-5717-4562-b3fc-2c963f66afa7", "name": "Pix", "isActive": true }
                        ]
                        """,
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
