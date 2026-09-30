using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Traffic;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Traffic;

public class TrafficInvestmentSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ProjectId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public TrafficInvestmentSectionTests()
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
        Services.AddScoped<ITrafficInvestmentApiService, TrafficInvestmentApiService>();
        Services.AddScoped<IProjectApiService, ProjectApiService>();
    }

    [Fact]
    public async Task TrafficInvestmentSection_Admin_ShowsCreateButtonAndTaxAmount()
    {
        ConfigureApiResponses();
        await AuthenticateAsAdminAsync();

        var cut = Render<TrafficInvestmentSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo investimento");
            cut.Markup.Should().Contain("Projeto Demo");
            cut.Markup.Should().Contain("R$ 121,50");
            cut.Find(".traffic-investment-filters-card").Should().NotBeNull();
            cut.Find(".traffic-investment-table-card").Should().NotBeNull();
            cut.FindAll(".traffic-investment-filters-card").Count.Should().Be(1);
            cut.FindAll(".traffic-investment-table-card").Count.Should().Be(1);
            cut.Find(".traffic-investment-table").Should().NotBeNull();
            cut.Find(".traffic-investment-table__money").Should().NotBeNull();
            cut.Find("thead").Should().NotBeNull();
        });
    }

    [Fact]
    public async Task TrafficInvestmentSection_Director_DoesNotShowCreateButton()
    {
        ConfigureApiResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<TrafficInvestmentSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Projeto Demo");
            cut.Markup.Should().NotContain("Novo investimento");
        });
    }

    [Fact]
    public async Task TrafficInvestmentSection_EmptyList_ShowsEmptyState()
    {
        ConfigureApiResponses(investmentListJson: "[]");
        await AuthenticateAsAdminAsync();

        var cut = Render<TrafficInvestmentSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum investimento encontrado");
        });
    }

    private void ConfigureApiResponses(string investmentListJson = "")
    {
        investmentListJson = string.IsNullOrWhiteSpace(investmentListJson)
            ? CreateInvestmentListJson()
            : investmentListJson;

        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/projects")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateProjectListJson())
                };
            }

            if (request.RequestUri!.AbsolutePath == "/api/v1/traffic-investments")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(investmentListJson)
                };
            }

            if (request.RequestUri!.AbsolutePath == "/api/v1/traffic-deposits")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
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

    private static string CreateInvestmentListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = DateTime.UtcNow.Month,
                year = DateTime.UtcNow.Year,
                monthlyTarget = 4000m,
                monthlyTotals = new
                {
                    requestedAmount = 0m,
                    depositedAmount = 0m,
                    spentAmount = 1000m,
                    taxAmount = 121.50m,
                    totalAmount = 1121.50m,
                    balance = -1121.50m
                }
            }
        });
}
