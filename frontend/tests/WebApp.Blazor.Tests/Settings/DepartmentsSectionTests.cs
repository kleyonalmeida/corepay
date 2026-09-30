using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Settings;
using WebApp.Blazor.Services;
using SettingsPage = WebApp.Blazor.Pages.Settings;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.SettingsUi;

public class DepartmentsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public DepartmentsSectionTests()
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
        Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
    }

    [Fact]
    public async Task DepartmentsSection_Admin_ShowsCreateButtonAndList()
    {
        ConfigureDepartmentsResponse(HttpStatusCode.OK, CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<DepartmentsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo setor");
            cut.Markup.Should().Contain("Gerência");
            cut.Markup.Should().Contain("Tipster");
        });
    }

    [Fact]
    public async Task DepartmentsSection_EmptyList_ShowsEmptyState()
    {
        ConfigureDepartmentsResponse(HttpStatusCode.OK, "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<DepartmentsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum setor cadastrado");
        });
    }

    [Fact]
    public async Task SettingsPage_Director_DoesNotRenderDepartmentsSection()
    {
        ConfigurePaymentMethodsResponse(HttpStatusCode.OK, "[]");
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SettingsPage>());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Configurações");
            cut.Markup.Should().NotContain("Setores");
            cut.Markup.Should().NotContain("Novo setor");
            cut.Markup.Should().Contain("Formas de pagamento");
        });

        _httpHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task DepartmentsSection_CreateTipster_SubmitsTipsterPayload()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/departments");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateDepartmentJson("Tipster", "tipster"))
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<DepartmentsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo setor"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo setor") == true).Click();

        cut.WaitForAssertion(() => cut.Find("input[placeholder='Nome do setor']").Should().NotBeNull());

        cut.Find("input[placeholder='Nome do setor']").Input("Tipster");
        cut.Find("select").Change(CalculationProfile.Tipster.ToString());
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"name\":\"Tipster\"");
            capturedBody.Should().Contain("\"calculationType\":\"tipster\"");
        });
    }

    [Fact]
    public async Task DepartmentsSection_CreateGerencia_SubmitsManagementPayload()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateDepartmentJson("Gerência", "management"))
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<DepartmentsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo setor"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo setor") == true).Click();

        cut.Find("input[placeholder='Nome do setor']").Input("Gerência");
        cut.Find("select").Change(CalculationProfile.Management.ToString());
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"calculationType\":\"management\"");
            using var document = JsonDocument.Parse(capturedBody!);
            document.RootElement.GetProperty("name").GetString().Should().Be("Gerência");
        });
    }

    [Fact]
    public async Task DepartmentsSection_Conflict_ShowsDuplicateMessage()
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    """{"error":"departments.duplicate","message":"Department already exists."}""")
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<DepartmentsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo setor"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo setor") == true).Click();
        cut.Find("input[placeholder='Nome do setor']").Input("Gerência");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Já existe um setor com este nome.");
        });
    }

    private void ConfigureDepartmentsResponse(HttpStatusCode status, string body)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/departments");
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            };
        });
    }

    private void ConfigurePaymentMethodsResponse(HttpStatusCode status, string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath == "/api/v1/payment-methods")
            {
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private Task AuthenticateAsAdminAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

    private static string CreateDepartmentListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                name = "Gerência",
                calculationType = "management",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200_000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            },
            new
            {
                id = Guid.NewGuid(),
                name = "Tipster",
                calculationType = "tipster",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200_000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            }
        });

    private static string CreateDepartmentJson(string name, string calculationType) =>
        JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            name,
            calculationType,
            goalBonusPercentage = 0m,
            lowRevenueThreshold = 200_000m,
            lowRevenueBonusPct = 0.4m,
            description = (string?)null,
            isActive = true,
            isAllocatedFixed = false,
            routesFixedToLimaKarttos = false
        });
}
