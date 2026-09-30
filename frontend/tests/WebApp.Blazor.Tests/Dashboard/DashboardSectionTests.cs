using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Dashboard;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Dashboard;

public sealed class DashboardSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public DashboardSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IDashboardApiService, DashboardApiService>();
    }

    [Fact]
    public void DashboardSection_ShowsStatsFiltersAndRecentPayrolls()
    {
        ConfigureResponse(CreateDashboardJson());

        var cut = Render<DashboardSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Total de Folhas de Pagamento");
            cut.Markup.Should().Contain("Aguardando");
            cut.Markup.Should().Contain("R$ 4.200,00");
            cut.Markup.Should().Contain("12");
            cut.Markup.Should().Contain("Folhas de pagamento recentes");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("Ago/2026");
            cut.Find(".dashboard-stats-grid--primary").Should().NotBeNull();
            cut.FindAll(".stat-card").Count.Should().BeGreaterThanOrEqualTo(4);
            cut.Find(".dashboard-recent-table").Should().NotBeNull();
        });
    }

    [Fact]
    public void DashboardSection_WithoutPermissions_HidesProtectedWidgets()
    {
        ConfigureResponse("""
            {
              "month": 8,
              "year": 2026,
              "payrollStats": null,
              "activeCollaborators": null,
              "recentPayrolls": null
            }
            """);

        var cut = Render<DashboardSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum indicador disponível");
            cut.Markup.Should().NotContain("Total de Folhas de Pagamento");
            cut.Markup.Should().NotContain("Folhas de pagamento recentes");
        });
    }

    [Fact]
    public void DashboardSection_Error_ShowsErrorState()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var cut = Render<DashboardSection>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Erro ao carregar dashboard"));
    }

    [Fact]
    public void DashboardSection_ClickRecentPayroll_NavigatesToDetail()
    {
        ConfigureResponse(CreateDashboardJson());
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<DashboardSection>();

        cut.WaitForElement("tr.dashboard-recent-table__row").Click();

        navigation.Uri.Should().EndWith("/payrolls/8fa85f64-5717-4562-b3fc-2c963f66afa7");
    }

    private void ConfigureResponse(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/dashboard")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private static string CreateDashboardJson() => """
        {
          "month": 8,
          "year": 2026,
          "payrollStats": {
            "totalPayrolls": 4,
            "awaitingApproval": 1,
            "approved": 2,
            "rejected": 1,
            "totalToPay": 4200,
            "totalPaid": 2800
          },
          "activeCollaborators": 12,
          "recentPayrolls": [
            {
              "id": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
              "departmentId": "9fa85f64-5717-4562-b3fc-2c963f66afa7",
              "departmentName": "Analistas Comerciais",
              "month": 8,
              "year": 2026,
              "status": "approved",
              "totalAmount": 7000,
              "entryCount": 2
            }
          ]
        }
        """;
}
