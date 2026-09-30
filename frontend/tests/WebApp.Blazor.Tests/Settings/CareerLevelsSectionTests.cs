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

public class CareerLevelsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid DepartmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");

    public CareerLevelsSectionTests()
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
        Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
    }

    [Fact]
    public async Task CareerLevelsSection_Admin_ShowsCreateButtonAndList()
    {
        ConfigureHttpResponses();
        await AuthenticateAsAdminAsync();

        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo nível");
            cut.Markup.Should().Contain("Analista Comercial Júnior");
            cut.Markup.Should().Contain("Analista comercial");
        });
    }

    [Fact]
    public async Task CareerLevelsSection_EmptyList_ShowsEmptyState()
    {
        ConfigureHttpResponses(careerLevelsJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum nível cadastrado");
        });
    }

    [Fact]
    public async Task SettingsPage_Director_DoesNotRenderCareerLevelsSection()
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
            cut.Markup.Should().NotContain("Níveis de carreira");
            cut.Markup.Should().NotContain("Novo nível");
            cut.Markup.Should().Contain("Formas de pagamento");
        });

        _httpHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task CareerLevelsSection_CreateAnalystJunior_SubmitsExpectedPayload()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateDepartmentsJson())
                };
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/career-levels")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/career-levels");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateCareerLevelJson())
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo nível"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo nível") == true).Click();

        cut.WaitForAssertion(() => cut.Find("input[placeholder='Nome do nível']").Should().NotBeNull());

        cut.Find("input[placeholder='Nome do nível']").Input("Analista Comercial Júnior");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();

            using var document = JsonDocument.Parse(capturedBody!);
            var root = document.RootElement;
            root.GetProperty("name").GetString().Should().Be("Analista Comercial Júnior");
            root.GetProperty("profile").GetString().Should().Be("commercialAnalyst");
            root.GetProperty("ftdRateBase").GetDecimal().Should().Be(2m);
            root.GetProperty("salesPctBase").GetDecimal().Should().Be(4m);
            root.GetProperty("ftdBonusEvery").ValueKind.Should().Be(JsonValueKind.Number);
            root.GetProperty("ftdBonusEvery").GetInt32().Should().Be(250);
            root.GetProperty("ftdBonusValue").GetDecimal().Should().Be(350m);
        });
    }

    [Fact]
    public async Task CareerLevelsSection_CommercialAnalystProfile_ShowsFtdBlock()
    {
        ConfigureHttpResponses(careerLevelsJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo nível"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo nível") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Taxa FTD base (R$)");
            cut.Markup.Should().Contain("% vendas base");
        });
    }

    [Fact]
    public async Task CareerLevelsSection_PaidTrafficProfile_ShowsCpaBlock()
    {
        ConfigureHttpResponses(careerLevelsJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo nível"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo nível") == true).Click();

        cut.WaitForAssertion(() => cut.Find("select").Should().NotBeNull());

        var profileSelect = cut.FindAll("select").First(s => s.InnerHtml.Contains("Tráfego pago"));
        profileSelect.Change(CalculationProfile.PaidTraffic.ToString());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("CPA Esportiva");
            cut.Markup.Should().Contain("CPA Betano");
            cut.Markup.Should().Contain("não entram no cálculo automático");
        });
    }

    [Fact]
    public async Task CareerLevelsSection_Conflict_ShowsDuplicateMessage()
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                var path = request.RequestUri!.AbsolutePath;
                if (path == "/api/v1/departments")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(CreateDepartmentsJson())
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    """{"error":"careerlevels.duplicate","message":"Career level already exists."}""")
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<CareerLevelsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo nível"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo nível") == true).Click();
        cut.Find("input[placeholder='Nome do nível']").Input("Analista Comercial Júnior");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Já existe um nível com este nome");
        });
    }

    private void ConfigureHttpResponses(string? careerLevelsJson = null)
    {
        careerLevelsJson ??= CreateCareerLevelListJson();
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/career-levels")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(careerLevelsJson)
                };
            }

            if (path == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateDepartmentsJson())
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
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

    private static string CreateDepartmentsJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = DepartmentId,
                name = "Analistas Comerciais",
                calculationType = "commercialAnalyst",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200_000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            }
        });

    private static string CreateCareerLevelListJson() =>
        JsonSerializer.Serialize(new[] { CreateCareerLevelObject() });

    private static string CreateCareerLevelJson() =>
        JsonSerializer.Serialize(CreateCareerLevelObject());

    private static object CreateCareerLevelObject() =>
        new
        {
            id = Guid.NewGuid(),
            name = "Analista Comercial Júnior",
            departmentId = DepartmentId,
            profile = "commercialAnalyst",
            isActive = true,
            baseSalary = 1500m,
            commissionWithoutGoalPct = 0m,
            commissionWithGoalPct = 0m,
            commissionWithSuperGoalPct = 0m,
            groupCommissionPerPercent = 0m,
            groupCommissionPer20Percent = 0m,
            defaultCpaValue = 0m,
            goalBonusValue = 0m,
            ftdRateBase = 2m,
            ftdRateWithGoal = 2.5m,
            ftdRateWithSuperGoal = 3m,
            ftdSuperbetRate = 5m,
            ftdBonusEvery = 250,
            ftdBonusValue = 350m,
            salesPctBase = 4m,
            salesPctWithGoal = 5m,
            salesPctWithSuperGoal = 6m,
            salesBonusEvery = 20_000m,
            salesBonusValue = 250m,
            revPct = 1m,
            betanoInternaValue = 200m,
            betanoMundoBetValue = 70m,
            supFtdSuperbetNoGoal = 0m,
            supFtdSuperbetWithGoal = 0m,
            supFtdOtherNoGoal = 0m,
            supFtdOtherWithGoal = 0m,
            supSalesPctNoGoal = 0m,
            supSalesPctWithGoal = 0m,
            supRevPct = 0m,
            netRevenueFactor = 0m,
            netRevenuePctNoGoal = 0m,
            netRevenuePctWithGoal = 0m,
            trafficInvestmentCommissionPct = 0m,
            trafficCpaEsportiva = 0m,
            trafficCpaStake = 0m,
            trafficCpaBetano = 0m,
            trafficCpaBetMgm = 0m,
            trafficCpaNovibet = 0m,
            trafficCpaBetFair = 0m,
            trafficCpaBlaze = 0m,
            trafficCpaSuperbet = 0m,
            trafficCpaHiperbet = 0m,
            trafficSupBonus = 0m,
            trafficSupCommissionPct = 0m
        };
}
