using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.ProjectRevenues;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.ProjectRevenues;

public class ProjectRevenuesSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ProjectId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public ProjectRevenuesSectionTests()
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
        Services.AddScoped<IProjectRevenueApiService, ProjectRevenueApiService>();
        Services.AddScoped<IProjectApiService, ProjectApiService>();
    }

    [Fact]
    public async Task ProjectRevenuesSection_Financial_ShowsCreateButtonAndList()
    {
        ConfigureApiResponses();
        await AuthenticateAsFinancialAsync();

        var cut = Render<ProjectRevenuesSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo faturamento");
            cut.Markup.Should().Contain("Projeto Demo");
            cut.Markup.Should().Contain("R$ 1.500,00");
        });
    }

    [Fact]
    public async Task ProjectRevenuesSection_Director_DoesNotShowCreateButton()
    {
        ConfigureApiResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<ProjectRevenuesSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Projeto Demo");
            cut.Markup.Should().NotContain("Novo faturamento");
        });
    }

    [Fact]
    public async Task ProjectRevenuesSection_EmptyList_ShowsEmptyState()
    {
        ConfigureApiResponses(revenueListJson: "[]");
        await AuthenticateAsFinancialAsync();

        var cut = Render<ProjectRevenuesSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum faturamento encontrado");
        });
    }

    [Fact]
    public async Task ProjectRevenuesSection_Create_SubmitsIgamingAndVendasPayload()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/projects")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateProjectListJson())
                };
            }

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
                Content = new StringContent(CreateRevenueJson())
            };
        });

        await AuthenticateAsFinancialAsync();
        var cut = Render<ProjectRevenuesSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo faturamento"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo faturamento") == true).Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Selecione um projeto"));

        var dialogProjectSelect = cut.FindAll("select")
            .First(select => select.InnerHtml.Contains("Selecione um projeto"));
        dialogProjectSelect.Change(ProjectId.ToString());

        var moneyInputs = cut.FindAll("input.ui-input--money");
        moneyInputs[0].Input("100000");
        moneyInputs[1].Input("50000");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Registrar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"valueIgaming\":1000");
            capturedBody.Should().Contain("\"valueVendas\":500");
        });
    }

    private void ConfigureApiResponses(string revenueListJson = "")
    {
        revenueListJson = string.IsNullOrWhiteSpace(revenueListJson) ? CreateRevenueListJson() : revenueListJson;

        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/projects")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateProjectListJson())
                };
            }

            if (request.RequestUri!.AbsolutePath == "/api/v1/project-revenues")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(revenueListJson)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private Task AuthenticateAsFinancialAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);

    private static string CreateProjectListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ProjectId,
                name = "Projeto Demo",
                client = (string?)null,
                platform = "lastlink",
                isActive = true,
                isDefaultAllocationTarget = false,
                excludesGoalBonus = false,
                excludesSupervisorFixedAllocation = false
            }
        });

    private static string CreateRevenueListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = DateTime.UtcNow.Month,
                year = DateTime.UtcNow.Year,
                valueIgaming = 1000m,
                valueVendas = 500m,
                value = 1500m,
                groupPercentage = 0m,
                notes = (string?)null
            }
        });

    private static string CreateRevenueJson() =>
        JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            projectId = ProjectId,
            projectName = "Projeto Demo",
            month = DateTime.UtcNow.Month,
            year = DateTime.UtcNow.Year,
            valueIgaming = 1000m,
            valueVendas = 500m,
            value = 1500m,
            groupPercentage = 0m,
            notes = (string?)null
        });
}
