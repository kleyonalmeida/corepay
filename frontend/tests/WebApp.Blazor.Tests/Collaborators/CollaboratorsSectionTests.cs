using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Collaborators;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Tests.Common;

namespace WebApp.Blazor.Tests.Collaborators;

public class CollaboratorsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public CollaboratorsSectionTests()
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
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
        Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
        Services.AddScoped<ICareerLevelApiService, CareerLevelApiService>();
    }

    [Fact]
    public async Task CollaboratorsSection_Admin_ShowsNovoColaboradorButton()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreateCollaboratorListJson(), CreateDepartmentListJson(), CreateCareerLevelListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo colaborador");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_Admin_ShowsCollaboratorsAndEditButton()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreateCollaboratorListJson(), CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana Comercial");
            cut.Markup.Should().Contain("Analista Comercial Júnior");
            cut.Markup.Should().Contain("11999990001");
            cut.Markup.Should().Contain("R$ 3.500,00");
            cut.Markup.Should().Contain("01/03/2024");
            cut.Markup.Should().Contain("Editar");
            cut.Markup.Should().Contain("collaborators-table__row");
            cut.Markup.Should().Contain("collaborators-table__row-action");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_Director_DoesNotShowEditButton()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreateCollaboratorListJson(), CreateDepartmentListJson());
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana Comercial");
            cut.Markup.Should().NotContain("Editar");
            cut.Markup.Should().NotContain("collaborators-table__row-action");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_InactiveCollaborator_ShowsDismissalDateAndInactiveBadge()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreateInactiveCollaboratorListJson(), CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bruno Tráfego");
            cut.Markup.Should().Contain("15/03/2025");
            cut.Markup.Should().Contain("text-dismissal-date");
            cut.Markup.Should().Contain("Inativo");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_EmptyList_ShowsEmptyState()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CollaboratorTestJson.WrapList([]), CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum colaborador encontrado");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_LoadError_ShowsEmptyStateWithError()
    {
        ConfigureApiResponses(HttpStatusCode.InternalServerError, null, CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Erro ao carregar colaboradores");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_CreateInactive_SubmitsDismissalDate()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateDepartmentListJson())
                };
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/career-levels")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateCareerLevelListJson())
                };
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CollaboratorTestJson.WrapList([]))
                };
            }

            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/collaborators");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateInactiveCollaboratorJson())
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo colaborador"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo colaborador") == true).Click();

        cut.WaitForAssertion(() => cut.Find("input[placeholder='Nome completo']").Should().NotBeNull());
        cut.Find("input[placeholder='Nome completo']").Input("Bruno Tráfego");
        var departmentId = JsonDocument.Parse(CreateDepartmentListJson()).RootElement[0].GetProperty("id").GetString()!;
        cut.FindAll("select").First(select => select.InnerHtml.Contains("Selecione um setor")).Change(departmentId);
        cut.Find("button[aria-label='Ativo']").Click();
        cut.FindAll("input[type='date']").Last().Input("2025-03-15");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"isActive\":false");
            capturedBody.Should().Contain("\"dismissalDate\":\"2025-03-15\"");
        });
    }

    [Fact]
    public async Task CollaboratorsSection_StatusFilter_RequestsActiveOnly()
    {
        string? capturedPath = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateDepartmentListJson())
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/career-levels")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateCareerLevelListJson())
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                capturedPath = request.RequestUri.PathAndQuery;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CollaboratorTestJson.WrapList([]))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<CollaboratorsSection>();

        cut.WaitForAssertion(() => cut.FindAll("select").Count.Should().BeGreaterThanOrEqualTo(2));
        cut.FindAll("select")[1].Change("active");

        cut.WaitForAssertion(() =>
        {
            capturedPath.Should().Contain("isActive=true");
        });
    }

    private void ConfigureApiResponses(
        HttpStatusCode collaboratorsStatus,
        string? collaboratorsBody,
        string departmentsBody,
        string? careerLevelsBody = "[]")
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/departments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(departmentsBody)
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/career-levels")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(careerLevelsBody!)
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                return collaboratorsBody is null
                    ? new HttpResponseMessage(collaboratorsStatus)
                    : new HttpResponseMessage(collaboratorsStatus)
                    {
                        Content = new StringContent(collaboratorsBody)
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

    private static string CreateCollaboratorListJson() =>
        CollaboratorTestJson.WrapList(
        [
            new
            {
                id = Guid.NewGuid(),
                name = "Ana Comercial",
                departmentId = Guid.NewGuid(),
                departmentName = "Analistas Comerciais",
                careerLevelId = Guid.NewGuid(),
                careerLevelName = "Analista Comercial Júnior",
                jobTitle = "Analista Comercial",
                admissionDate = "2024-03-01",
                dismissalDate = (string?)null,
                pixKey = "11999990001",
                baseSalary = 3500m,
                email = "ana@corepay.test",
                photoUrl = (string?)null,
                isActive = true,
                calculationProfileOverride = (string?)null
            }
        ]);

    private static string CreateInactiveCollaboratorListJson() =>
        CollaboratorTestJson.WrapList(
        [
            new
            {
                id = Guid.NewGuid(),
                name = "Bruno Tráfego",
                departmentId = Guid.NewGuid(),
                departmentName = "Tráfego Pago",
                careerLevelId = Guid.NewGuid(),
                careerLevelName = "Sênior",
                jobTitle = "Especialista de Tráfego",
                admissionDate = "2023-06-15",
                dismissalDate = "2025-03-15",
                pixKey = "11999990002",
                baseSalary = 4200m,
                email = "bruno@corepay.test",
                photoUrl = (string?)null,
                isActive = false,
                calculationProfileOverride = (string?)null
            }
        ]);

    private static string CreateInactiveCollaboratorJson() =>
        JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            name = "Bruno Tráfego",
            departmentId = Guid.NewGuid(),
            departmentName = "Tráfego Pago",
            careerLevelId = Guid.NewGuid(),
            careerLevelName = "Sênior",
            jobTitle = "Especialista de Tráfego",
            admissionDate = "2023-06-15",
            dismissalDate = "2025-03-15",
            pixKey = "11999990002",
            baseSalary = 4200m,
            email = "bruno@corepay.test",
            photoUrl = (string?)null,
            isActive = false,
            calculationProfileOverride = (string?)null
        });

    private static string CreateCareerLevelListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                name = "Analista Comercial Júnior",
                departmentId = Guid.NewGuid(),
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
                supFtdSuperbetNoGoal = 4m,
                supFtdSuperbetWithGoal = 5m,
                supFtdOtherNoGoal = 0.3m,
                supFtdOtherWithGoal = 0.5m,
                supSalesPctNoGoal = 0.5m,
                supSalesPctWithGoal = 0.8m,
                supRevPct = 10m,
                netRevenueFactor = 50m,
                netRevenuePctNoGoal = 1.2m,
                netRevenuePctWithGoal = 1.5m,
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
            }
        });

    private static string CreateDepartmentListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
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
}
