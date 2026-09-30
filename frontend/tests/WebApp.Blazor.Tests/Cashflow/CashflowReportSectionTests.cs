using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Cashflow;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Cashflow;

public class CashflowReportSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public CashflowReportSectionTests()
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
        Services.AddScoped<ICashflowApiService, CashflowApiService>();
    }

    [Fact]
    public async Task CashflowReportSection_ShowsSummaryAndTables()
    {
        ConfigureReportResponse();
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowReportSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Por projeto");
            cut.Markup.Should().Contain("Por forma de pagamento");
            cut.Markup.Should().Contain("R$ 750,00");
            cut.Markup.Should().Contain("Sem projeto");
            cut.Markup.Should().Contain("Pix");
        });
    }

    [Fact]
    public async Task CashflowReportSection_EmptyReport_ShowsEmptyStates()
    {
        ConfigureReportResponse(empty: true);
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowReportSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum lançamento no período");
            cut.Markup.Should().Contain("Nenhuma saída no período");
        });
    }

    [Fact]
    public async Task CashflowReportSection_NegativeSaldo_ShowsRedTone()
    {
        ConfigureReportResponse(saldo: -150m);
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowReportSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("-R$ 150,00");
        });
    }

    [Fact]
    public async Task CashflowSection_ShowsReportTab()
    {
        ConfigureListAndReportResponses();
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Relatório");
            cut.Markup.Should().Contain("Lançamentos");
        });
    }

    private async Task AuthenticateAsFinancialAsync()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);
    }

    private void ConfigureReportResponse(bool empty = false, decimal saldo = 750m)
    {
        var byProjectJson = empty
            ? "[]"
            : """
              [
                {
                  "projectId": null,
                  "projectName": "Sem projeto",
                  "totalEntradas": 200,
                  "totalSaidas": 0,
                  "saldo": 200,
                  "count": 1
                }
              ]
              """;
        var byPaymentMethodJson = empty
            ? "[]"
            : """
              [
                {
                  "paymentMethodId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
                  "paymentMethodName": "Pix",
                  "totalSaidas": 450,
                  "count": 2
                }
              ]
              """;
        var totalEntradas = empty ? 0 : 1200m;
        var totalSaidas = empty ? 0 : 450m;
        var count = empty ? 0 : 4;

        var reportJson = $$"""
                           {
                             "summary": {
                               "totalEntradas": {{totalEntradas}},
                               "totalSaidas": {{totalSaidas}},
                               "saldo": {{saldo}},
                               "count": {{count}}
                             },
                             "byProject": {{byProjectJson}},
                             "byPaymentMethod": {{byPaymentMethodJson}}
                           }
                           """;

        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath.Equals("/api/v1/cashflow/report", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(reportJson, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureListAndReportResponses()
    {
        var listJson = """
                       {
                         "summary": { "totalEntradas": 1000, "totalSaidas": 300, "saldo": 700, "count": 1 },
                         "items": [],
                         "filterOptions": { "projects": [], "departments": [], "paymentMethods": [] }
                       }
                       """;

        var reportJson = """
                         {
                           "summary": { "totalEntradas": 0, "totalSaidas": 0, "saldo": 0, "count": 0 },
                           "byProject": [],
                           "byPaymentMethod": []
                         }
                         """;

        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath.Equals("/api/v1/cashflow", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(listJson, System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath.Equals("/api/v1/cashflow/report", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(reportJson, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
