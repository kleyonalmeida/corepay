using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Financial;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Tests.Common;

namespace WebApp.Blazor.Tests.Financial;

public class FinancialSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public FinancialSectionTests()
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
        Services.AddScoped<IFinanceApiService, FinanceApiService>();
        Services.AddScoped<IPayrollApiService, PayrollApiService>();
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
    }

    [Fact]
    public async Task FinancialSection_FiltersRender_MonthYearDepartmentProject()
    {
        ConfigureSummaryResponse(CreateSummaryJson(includeUnapprovedEntry: false));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Mês");
            cut.Markup.Should().Contain("Ano");
            cut.Markup.Should().Contain("Setor");
            cut.Markup.Should().Contain("Projeto");
        });
    }

    [Fact]
    public async Task FinancialSection_DraftPayroll_ShowsOnlyApprovedEntry()
    {
        ConfigureSummaryResponse(CreateSummaryJson(includeUnapprovedEntry: false));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana Comercial");
            cut.Markup.Should().NotContain("Bruno Comercial");
            cut.Markup.Should().Contain("1 colaborador");
        });
    }

    [Fact]
    public async Task FinancialSection_ApprovedPayroll_ShowsAllEntries()
    {
        ConfigureSummaryResponse(CreateSummaryJson(includeUnapprovedEntry: true, status: "approved"));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana Comercial");
            cut.Markup.Should().Contain("Bruno Comercial");
            cut.Markup.Should().Contain("2 colaboradores");
        });
    }

    [Fact]
    public async Task FinancialSection_CommercialEntry_ShowsAmountToReceiveWithStrikethrough()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            totalAmount: 1200m,
            platformTotal: 400m,
            amountToReceive: 800m));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("A Receber");
            cut.Markup.Should().Contain("financial-amount__gross");
            cut.Markup.Should().Contain("financial-amount__receive");
            cut.Markup.Should().Contain("R$ 800,00");
            cut.Markup.Should().Contain("R$ 1.200,00");
        });
    }

    [Fact]
    public async Task FinancialSection_NonCommercialEntry_ShowsAmountWithoutStrikethrough()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            totalAmount: 3000m,
            platformTotal: 0m,
            amountToReceive: 3000m));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("A Receber");
            cut.Markup.Should().NotContain("financial-amount__gross");
            cut.Markup.Should().Contain("R$ 3.000,00");
        });
    }

    [Fact]
    public async Task FinancialSection_ShowsStatsAndProgressBar()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            totalAmount: 1200m,
            platformTotal: 400m,
            amountToReceive: 800m,
            paidCount: 0,
            entryCount: 1));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Total a pagar");
            cut.Markup.Should().Contain("Total pago");
            cut.Markup.Should().Contain("financial-progress");
            cut.Markup.Should().Contain("0/1");
        });
    }

    [Fact]
    public async Task FinancialSection_FinancialApproved_ShowsPayActions()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            payEntry: true,
            toggleNf: true));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Marcar pago");
            cut.Markup.Should().Contain("Marcar NF enviada");
        });
    }

    [Fact]
    public async Task FinancialSection_DirectorApproved_HidesPayActions()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            payEntry: false,
            toggleNf: false));
        await AuthenticateAsDirectorAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Marcar pago");
            cut.Markup.Should().NotContain("Marcar NF enviada");
        });
    }

    [Fact]
    public async Task FinancialSection_PayClick_ReloadsSummary()
    {
        var payrollId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var summaryCalls = 0;

        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/finance/summary")
            {
                summaryCalls++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateSummaryJson(
                        includeUnapprovedEntry: false,
                        status: "approved",
                        payrollId: payrollId,
                        entryId: entryId,
                        payEntry: true))
                };
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == $"/api/v1/payrolls/{payrollId}/entries/{entryId}/pay")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":"00000000-0000-0000-0000-000000000001","status":"approved"}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await AuthenticateAsFinancialAsync();
        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Marcar pago"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Marcar pago") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            summaryCalls.Should().BeGreaterThanOrEqualTo(2);
            cut.Markup.Should().Contain("Colaborador marcado como pago.");
        });
    }

    [Fact]
    public async Task FinancialSection_EmptyList_ShowsEmptyState()
    {
        ConfigureSummaryResponse(CreateEmptySummaryJson());
        await AuthenticateAsFinancialAsync();

        var cut = Render<FinancialSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhum colaborador a pagar");
        });
    }

    [Fact]
    public async Task FinancialSection_PaidEntry_ShowsPaidRowClass()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            isPaid: true,
            payEntry: true,
            toggleNf: true));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("financial-table__row--paid");
            cut.Markup.Should().Contain("Paga");
        });
    }

    [Fact]
    public async Task FinancialSection_ApiError_ShowsErrorEmptyState()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Erro ao carregar financeiro");
        });
    }

    [Fact]
    public async Task FinancialSection_TabsRender_ColaboradoresAndCustoPorProjeto()
    {
        ConfigureSummaryResponse(CreateSummaryJson(includeUnapprovedEntry: false));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Colaboradores");
            cut.Markup.Should().Contain("Custo por Projeto");
            cut.Markup.Should().Contain("role=\"tablist\"");
        });
    }

    [Fact]
    public async Task FinancialSection_ProjectCostTab_ShowsBreakdown()
    {
        var projectId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            projectTotals: [(projectId, 1200m)],
            projectName: "Lastlink Sample"));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Custo por Projeto"));

        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Custo por Projeto") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ana Comercial"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Ana Comercial") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Custo por projeto");
            cut.Markup.Should().Contain("Lastlink Sample");
            cut.Markup.Should().Contain("R$ 1.200,00");
        });
    }

    [Fact]
    public async Task FinancialSection_ProjectCostTab_EmptyProjectTotals_ShowsFallback()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            projectTotals: []));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Custo por Projeto") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ana Comercial"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Ana Comercial") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Sem alocação por projeto");
        });
    }

    [Fact]
    public async Task FinancialSection_TabSwitch_PreservesFilters()
    {
        ConfigureSummaryResponse(CreateEmptySummaryJson());
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();
        cut.WaitForAssertion(() => cut.FindAll("select").Count.Should().BeGreaterThanOrEqualTo(4));

        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Custo por Projeto") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("select").Count.Should().BeGreaterThanOrEqualTo(4);
            cut.Markup.Should().Contain("Mês");
            cut.Markup.Should().Contain("Projeto");
        });
    }

    [Fact]
    public async Task FinancialSection_FilterChange_ReloadsWithQuery()
    {
        string? capturedPath = null;
        _httpHandler.Configure(request =>
        {
            capturedPath = request.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateEmptySummaryJson())
            };
        });

        await AuthenticateAsFinancialAsync();
        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() => cut.FindAll("select").Count.Should().BeGreaterThanOrEqualTo(4));
        cut.FindAll("select")[0].Change("3");

        cut.WaitForAssertion(() =>
        {
            capturedPath.Should().Contain("month=3");
        });
    }

    private void ConfigureSummaryResponse(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/finance/summary")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
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

    private Task AuthenticateAsDirectorAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

    private static string CreateEmptySummaryJson() =>
        JsonSerializer.Serialize(new
        {
            payrolls = Array.Empty<object>(),
            filterOptions = new
            {
                departments = new[] { new { id = Guid.NewGuid(), name = "Analistas Comerciais" } },
                projects = new[] { new { id = Guid.NewGuid(), name = "Lastlink Sample" } }
            }
        });

    [Fact]
    public async Task FinancialSection_FinancialDraft_ShowsAddAvulsoButton()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "draft",
            addCollaborator: true));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Adicionar colaborador avulso");
        });
    }

    [Fact]
    public async Task FinancialSection_Director_HidesAddAvulsoButton()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            addCollaborator: true));
        await AuthenticateAsDirectorAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Adicionar colaborador avulso");
        });
    }

    [Fact]
    public async Task FinancialSection_Admin_ShowsDeleteButton()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            deletePayroll: true));
        await AuthenticateAsAdminAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Excluir folha");
        });
    }

    [Fact]
    public async Task FinancialSection_Financial_HidesDeleteButton()
    {
        ConfigureSummaryResponse(CreateSummaryJson(
            includeUnapprovedEntry: false,
            status: "approved",
            deletePayroll: true));
        await AuthenticateAsFinancialAsync();

        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Excluir folha");
        });
    }

    [Fact]
    public async Task FinancialSection_AddAvulso_Success_ReloadsSummary()
    {
        var payrollId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var summaryCalls = 0;

        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/finance/summary")
            {
                summaryCalls++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateSummaryJson(
                        includeUnapprovedEntry: false,
                        status: "draft",
                        payrollId: payrollId,
                        addCollaborator: true))
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == $"/api/v1/payrolls/{payrollId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($$"""
                    {
                      "id": "{{payrollId}}",
                      "departmentId": "11111111-1111-1111-1111-111111111111",
                      "departmentName": "Analistas Comerciais",
                      "month": 4,
                      "year": 2028,
                      "status": "draft",
                      "totalAmount": 1200,
                      "entryCount": 1,
                      "entries": [],
                      "editorOptions": { "projects": [], "departments": [], "careerLevels": [] },
                      "allowedActions": { "addCollaborator": true }
                    }
                    """)
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/collaborators")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CollaboratorTestJson.WrapList(
                    [
                        new
                        {
                            id = collaboratorId,
                            name = "Bruno Avulso",
                            departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            departmentName = "Analistas Comerciais",
                            careerLevelName = "Analista Júnior",
                            isActive = true
                        }
                    ]))
                };
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == $"/api/v1/payrolls/{payrollId}/entries")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($$"""{"id":"{{payrollId}}","status":"draft"}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await AuthenticateAsFinancialAsync();
        var cut = RenderAndExpandFinancialSection();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Adicionar colaborador avulso"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Adicionar colaborador avulso") == true)
            .Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Bruno Avulso"));
        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Bruno Avulso") == true)
            .Click();

        cut.WaitForAssertion(() =>
        {
            summaryCalls.Should().BeGreaterThanOrEqualTo(2);
            cut.Markup.Should().Contain("Colaborador adicionado à lista a pagar.");
        });
    }

    private Task AuthenticateAsAdminAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

    private Bunit.IRenderedComponent<FinancialSection> RenderAndExpandFinancialSection()
    {
        var cut = Render<FinancialSection>();
        foreach (var header in cut.FindAll("button.financial-payroll-card__header"))
        {
            header.Click();
        }

        return cut;
    }

    private static string CreateSummaryJson(
        bool includeUnapprovedEntry,
        string status = "draft",
        decimal totalAmount = 1200m,
        decimal platformTotal = 0m,
        decimal amountToReceive = 1200m,
        int paidCount = 0,
        int entryCount = 1,
        bool payEntry = false,
        bool toggleNf = false,
        bool addCollaborator = false,
        bool deletePayroll = false,
        bool isPaid = false,
        Guid? payrollId = null,
        Guid? entryId = null,
        (Guid ProjectId, decimal Amount)[]? projectTotals = null,
        string projectName = "Lastlink Sample")
    {
        payrollId ??= Guid.NewGuid();
        entryId ??= Guid.NewGuid();
        var filterProjectId = projectTotals?.FirstOrDefault().ProjectId ?? Guid.NewGuid();

        var entries = new List<object>
        {
            new
            {
                id = entryId,
                collaboratorId = Guid.NewGuid(),
                collaboratorName = "Ana Comercial",
                careerLevelName = "Analista Júnior",
                pixKey = "11999990001",
                totalAmount,
                platformTotal,
                amountToReceive,
                isApproved = true,
                isPaid,
                nfSent = false,
                projectTotals = (projectTotals ?? [])
                    .Select(t => new { projectId = t.ProjectId, amount = t.Amount })
                    .ToArray()
            }
        };

        if (includeUnapprovedEntry)
        {
            entries.Add(new
            {
                id = Guid.NewGuid(),
                collaboratorId = Guid.NewGuid(),
                collaboratorName = "Bruno Comercial",
                careerLevelName = "Analista Júnior",
                pixKey = "11988887777",
                totalAmount = 900m,
                platformTotal = 0m,
                amountToReceive = 900m,
                isApproved = false,
                isPaid = false,
                nfSent = false
            });
            entryCount = 2;
        }

        return JsonSerializer.Serialize(new
        {
            payrolls = new[]
            {
                new
                {
                    id = payrollId,
                    departmentId = Guid.NewGuid(),
                    departmentName = "Analistas Comerciais",
                    month = 4,
                    year = 2028,
                    status,
                    grossTotal = totalAmount,
                    platformTotal,
                    amountToReceive,
                    paidAmount = 0m,
                    paidCount,
                    entryCount,
                    allowedActions = new
                    {
                        approvePayroll = false,
                        rejectPayroll = false,
                        approveEntry = false,
                        edit = false,
                        payPayroll = payEntry,
                        payEntry,
                        toggleNf,
                        postApprovalAdjustments = payEntry,
                        delete = deletePayroll,
                        recalculate = false,
                        addCollaborator
                    },
                    entries
                }
            },
            filterOptions = new
            {
                departments = new[] { new { id = Guid.NewGuid(), name = "Analistas Comerciais" } },
                projects = new[] { new { id = filterProjectId, name = projectName } }
            }
        });
    }
}
