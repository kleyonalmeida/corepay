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

public class ProjectsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public ProjectsSectionTests()
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
        Services.AddScoped<IProjectApiService, ProjectApiService>();
        Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
    }

    [Fact]
    public async Task ProjectsSection_Admin_ShowsCreateButtonAndList()
    {
        ConfigureProjectsResponse(HttpStatusCode.OK, CreateProjectListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<ProjectsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo projeto");
            cut.Markup.Should().Contain("Lima Karttos");
            cut.Markup.Should().Contain("Projeto Hubla Demo");
        });
    }

    [Fact]
    public async Task ProjectsSection_EmptyList_ShowsEmptyState()
    {
        ConfigureProjectsResponse(HttpStatusCode.OK, "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<ProjectsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum projeto cadastrado");
        });
    }

    [Fact]
    public async Task SettingsPage_Director_DoesNotRenderProjectsSection()
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
            cut.Markup.Should().NotContain("Projetos");
            cut.Markup.Should().NotContain("Novo projeto");
            cut.Markup.Should().Contain("Formas de pagamento");
        });

        _httpHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ProjectsSection_CreateHubla_SubmitsHublaPayload()
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
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/projects");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateProjectJson("Projeto Hubla Demo", "hubla"))
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<ProjectsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo projeto"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo projeto") == true).Click();

        cut.WaitForAssertion(() => cut.Find("input[placeholder='Nome do projeto']").Should().NotBeNull());

        cut.Find("input[placeholder='Nome do projeto']").Input("Projeto Hubla Demo");
        cut.Find("select").Change(ProjectPlatform.Hubla.ToString());
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"name\":\"Projeto Hubla Demo\"");
            capturedBody.Should().Contain("\"platform\":\"hubla\"");
        });
    }

    [Fact]
    public async Task ProjectsSection_CreateLimaKarttos_SubmitsDefaultAllocationTarget()
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
                Content = new StringContent(CreateProjectJson("Lima Karttos", "lastlink", isDefaultAllocationTarget: true))
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<ProjectsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo projeto"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo projeto") == true).Click();

        cut.Find("input[placeholder='Nome do projeto']").Input("Lima Karttos");
        cut.FindAll("button[role='switch']")
            .First(button => button.GetAttribute("aria-label")?.Contains("Alvo padrão de rateio") == true)
            .Click();
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("\"isDefaultAllocationTarget\":true");
            using var document = JsonDocument.Parse(capturedBody!);
            document.RootElement.GetProperty("name").GetString().Should().Be("Lima Karttos");
        });
    }

    [Fact]
    public async Task ProjectsSection_Conflict_ShowsDuplicateMessage()
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
                    """{"error":"projects.duplicate","message":"Project already exists."}""")
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<ProjectsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Novo projeto"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Novo projeto") == true).Click();
        cut.Find("input[placeholder='Nome do projeto']").Input("Lima Karttos");
        cut.FindAll("button").First(button => button.TextContent?.Contains("Salvar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Já existe um projeto com este nome.");
        });
    }

    private void ConfigureProjectsResponse(HttpStatusCode status, string body)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/projects");
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

    private static string CreateProjectListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                name = "Lima Karttos",
                client = (string?)null,
                platform = "lastlink",
                isActive = true,
                isDefaultAllocationTarget = true,
                excludesGoalBonus = false,
                excludesSupervisorFixedAllocation = false
            },
            new
            {
                id = Guid.NewGuid(),
                name = "Projeto Hubla Demo",
                client = (string?)"Cliente Hubla",
                platform = "hubla",
                isActive = true,
                isDefaultAllocationTarget = false,
                excludesGoalBonus = false,
                excludesSupervisorFixedAllocation = false
            }
        });

    private static string CreateProjectJson(
        string name,
        string platform,
        bool isDefaultAllocationTarget = false) =>
        JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            name,
            client = (string?)null,
            platform,
            isActive = true,
            isDefaultAllocationTarget,
            excludesGoalBonus = false,
            excludesSupervisorFixedAllocation = false
        });
}
