using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Cashflow;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Cashflow;

public class CashflowSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public CashflowSectionTests()
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
    public async Task CashflowSection_Financial_ShowsCreateButtonAndSummary()
    {
        ConfigureApiResponses();
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Novo lançamento");
            cut.Markup.Should().Contain("R$ 700,00");
            cut.Markup.Should().Contain("Entradas");
            cut.Find(".cashflow-stats-grid").Should().NotBeNull();
            cut.Find(".stat-card__icon--emerald").Should().NotBeNull();
            cut.Find(".stat-card__icon--red").Should().NotBeNull();
            cut.Find(".stat-card__icon--blue").Should().NotBeNull();
        });
    }

    [Fact]
    public async Task CashflowSection_Director_DoesNotShowCreateButton()
    {
        ConfigureApiResponses();
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("R$ 700,00");
            cut.Markup.Should().NotContain("Novo lançamento");
        });
    }

    [Fact]
    public async Task CashflowSection_EmptyList_ShowsEmptyState()
    {
        ConfigureApiResponses(emptyItems: true);
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum lançamento encontrado");
        });
    }

    [Fact]
    public async Task CashflowSection_DeleteClick_OpensConfirmationDialog()
    {
        ConfigureApiResponses();
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Excluir") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Excluir lançamento");
            cut.Markup.Should().Contain("Esta ação não pode ser desfeita");
        });
    }

    [Fact]
    public async Task CashflowSection_DeleteCancel_DoesNotCallDelete()
    {
        var deleteCalls = 0;
        ConfigureApiResponses(onRequest: request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deleteCalls++;
            }

            return null;
        });
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Excluir") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir lançamento"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Cancelar") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            deleteCalls.Should().Be(0);
            cut.Markup.Should().NotContain("Excluir lançamento");
        });
    }

    [Fact]
    public async Task CashflowSection_DeleteConfirm_CallsDeleteAndReloads()
    {
        var deleteCalls = 0;
        var listCalls = 0;
        ConfigureApiResponses(onRequest: request =>
        {
            if (request.Method == HttpMethod.Delete
                && request.RequestUri!.AbsolutePath == "/api/v1/cashflow/7fa85f64-5717-4562-b3fc-2c963f66afa6")
            {
                deleteCalls++;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/cashflow")
            {
                listCalls++;
            }

            return null;
        });
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Excluir") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir lançamento"));
        cut.Find(".ui-dialog .ui-button--destructive").Click();

        cut.WaitForAssertion(() =>
        {
            deleteCalls.Should().Be(1);
            listCalls.Should().BeGreaterThanOrEqualTo(2);
            cut.Markup.Should().Contain("Lançamento excluído com sucesso.");
        });
    }

    [Fact]
    public async Task CashflowSection_DeleteError_KeepsDialogOpenWithMessage()
    {
        ConfigureApiResponses(onRequest: request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            return null;
        });
        await AuthenticateAsFinancialAsync();

        var cut = Render<CashflowSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Excluir") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Excluir lançamento"));
        cut.Find(".ui-dialog .ui-button--destructive").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Excluir lançamento");
            cut.Markup.Should().Contain("Você não tem permissão para excluir lançamentos.");
        });
    }

    private async Task AuthenticateAsFinancialAsync()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);
    }

    private void ConfigureApiResponses(
        bool emptyItems = false,
        Func<HttpRequestMessage, HttpResponseMessage?>? onRequest = null)
    {
        var itemsJson = emptyItems
            ? "[]"
            : """
              [
                {
                  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "type": "entrada",
                  "category": "plataforma",
                  "amount": 1000,
                  "transactionDate": "2026-09-10",
                  "month": 9,
                  "year": 2026,
                  "projectId": null,
                  "projectName": null,
                  "departmentId": null,
                  "departmentName": null,
                  "paymentMethodId": null,
                  "paymentMethodName": null,
                  "requester": null,
                  "purchaseLocation": null,
                  "installmentNumber": null,
                  "installmentTotal": null,
                  "compraId": null,
                  "attachmentUrl": null,
                  "notes": null
                }
              ]
              """;

        var listJson = $$"""
                         {
                           "summary": { "totalEntradas": 1000, "totalSaidas": 300, "saldo": 700, "count": 1 },
                           "items": {{itemsJson}},
                           "filterOptions": {
                             "projects": [],
                             "departments": [],
                             "paymentMethods": [{ "id": "8fa85f64-5717-4562-b3fc-2c963f66afa7", "name": "Pix" }]
                           }
                         }
                         """;

        _httpHandler.Configure(request =>
        {
            var custom = onRequest?.Invoke(request);
            if (custom is not null)
            {
                return custom;
            }

            if (request.Method == HttpMethod.Get &&
                request.RequestUri!.AbsolutePath.Equals("/api/v1/cashflow", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(listJson, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}
