using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Payroll;
using WebApp.Blazor.Pages.Payroll;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollDetailSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid PayrollId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public PayrollDetailSectionTests()
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
        Services.AddScoped<IPayrollApiService, PayrollApiService>();
    }

    [Fact]
    public async Task DetailSection_DraftWithWrite_ShowsEditButtonAndSummary()
    {
        ConfigurePayrollResponse(CreatePayrollJson("draft", includeEntry: true, allowedActions: PayrollAllowedActionsTestHelper.ForManagerDraft()));
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Detalhe da folha");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("Jan/2026");
            cut.Markup.Should().Contain("Submetido por: Maria Manager");
            cut.Markup.Should().Contain("R$");
            cut.Markup.Should().Contain("11999990001");
            cut.Markup.Should().Contain("Editar");
            cut.Markup.Should().Contain("payroll-detail");
        });
    }

    [Fact]
    public async Task DetailSection_PendingApproval_HidesEditButton()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "pendingApproval",
            includeEntry: true,
            allowedActions: PayrollAllowedActionsTestHelper.Create()));
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Editar");
            cut.Markup.Should().Contain("Aguard. Aprovação");
        });
    }

    [Fact]
    public async Task DetailSection_Rejected_ShowsRejectionComment()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "rejected",
            includeEntry: true,
            rejectionComment: "Ajustar FTD",
            allowedActions: PayrollAllowedActionsTestHelper.ForRejectedManager()));
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Reprovação:");
            cut.Markup.Should().Contain("Ajustar FTD");
            cut.Markup.Should().Contain("Editar");
        });
    }

    [Fact]
    public async Task DetailSection_PaidEntry_ShowsWorkflowChips()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "paid",
            includeEntry: true,
            isPaid: true,
            nfSent: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.Create(
                payPayroll: false,
                payEntry: false)));
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Aprovada");
            cut.Markup.Should().Contain("Paga");
            cut.Markup.Should().Contain("Enviada");
            cut.Markup.Should().Contain("payroll-detail-entry--paid");
        });
    }

    [Fact]
    public async Task DetailSection_ExpandEntry_ShowsBreakdown()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "draft",
            includeEntry: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForManagerDraft()));
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() => cut.Find(".payroll-detail-entry__header").Should().NotBeNull());

        cut.Find(".payroll-detail-entry__header").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Custo por projeto");
            cut.Markup.Should().Contain("Projeto Demo");
            cut.Markup.Should().Contain("Fixo / complemento");
        });
    }

    [Fact]
    public async Task DetailSection_DirectorReadOnly_HidesEditButton()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "draft",
            includeEntry: true,
            allowedActions: PayrollAllowedActionsTestHelper.Create()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() => cut.Markup.Should().NotContain("Editar"));
    }

    private void ConfigurePayrollResponse(string jsonBody)
    {
        _httpHandler.Configure(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{PayrollId}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonBody)
            };
        });
    }

    [Fact]
    public async Task DetailSection_FinancialPendingApproval_DoesNotShowApproveButtons()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "pendingApproval",
            includeEntry: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForPendingApprovalFinancial()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Aprovar");
            cut.Markup.Should().NotContain("Reprovar");
            cut.Markup.Should().NotContain("Marcar folha paga");
        });
    }

    [Fact]
    public async Task DetailSection_DirectorPendingApproval_ShowsApproveButNotPay()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "pendingApproval",
            includeEntry: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForPendingApprovalDirector()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Aprovar");
            cut.Markup.Should().Contain("Reprovar");
            cut.Markup.Should().NotContain("Marcar folha paga");
            cut.Markup.Should().NotContain("Marcar pago");
        });
    }

    [Fact]
    public async Task DetailSection_AdminApproved_ShowsPayAndDeleteActions()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "approved",
            includeEntry: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForApprovedAdmin()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Marcar folha paga");
            cut.Markup.Should().Contain("Excluir");
            cut.Markup.Should().NotContain("Aprovar");
        });
    }

    [Fact]
    public async Task DetailSection_FinancialApproved_ShowsPayActions()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "approved",
            includeEntry: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForApprovedFinancial()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Marcar folha paga");
            cut.Markup.Should().NotContain("Aprovar");
            cut.Markup.Should().NotContain("Reprovar");
        });
    }

    [Fact]
    public async Task DetailSection_DirectorApproved_HidesPayActions()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "approved",
            includeEntry: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForApprovedDirector()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Marcar folha paga");
            cut.Markup.Should().NotContain("Marcar pago");
            cut.Markup.Should().NotContain("Marcar NF enviada");
        });
    }

    [Fact]
    public async Task DetailSection_ExpandedEntry_ShowsEntryPayActionsWhenAllowed()
    {
        ConfigurePayrollResponse(CreatePayrollJson(
            "approved",
            includeEntry: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.ForApprovedAdmin()));
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() => cut.Find(".payroll-detail-entry__header").Should().NotBeNull());
        cut.Find(".payroll-detail-entry__header").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Marcar pago");
            cut.Markup.Should().Contain("Marcar NF enviada");
        });
    }

    [Fact]
    public async Task DetailSection_DirectorPendingApproval_ApproveInvokesApi()
    {
        var approvedJson = CreatePayrollJson(
            "approved",
            includeEntry: true,
            isApproved: true,
            allowedActions: PayrollAllowedActionsTestHelper.Create(
                payPayroll: true,
                payEntry: true,
                toggleNf: true));

        var approveCalled = false;
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath == $"/api/v1/payrolls/{PayrollId}/approve")
            {
                approveCalled = true;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(approvedJson)
                };
            }

            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{PayrollId}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreatePayrollJson(
                    "pendingApproval",
                    includeEntry: true,
                    allowedActions: PayrollAllowedActionsTestHelper.ForPendingApprovalDirector()))
            };
        });

        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<PayrollDetail>(parameters => parameters.Add(p => p.Id, PayrollId));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Aprovar"));
        cut.FindAll("button").First(button => button.TextContent!.Contains("Aprovar")).Click();

        cut.WaitForAssertion(() =>
        {
            approveCalled.Should().BeTrue();
            cut.Markup.Should().Contain("Folha aprovada com sucesso.");
        });
    }

    private static string CreatePayrollJson(
        string status,
        bool includeEntry = false,
        string? rejectionComment = null,
        bool isApproved = false,
        bool isPaid = false,
        bool nfSent = false,
        object? allowedActions = null)
    {
        object? entry = includeEntry
            ? new
            {
                id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                collaboratorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                collaboratorName = "Ana Comercial",
                careerLevelName = "Analista Júnior",
                pixKey = "11999990001",
                admissionDate = "2024-03-01",
                fullBaseSalary = 3500m,
                calculationProfile = "commercialAnalyst",
                isApproved,
                isPaid,
                nfSent,
                goalTier = "none",
                finalSalary = (decimal?)null,
                betanoInternaCount = 0,
                betanoMundoBetCount = 0,
                supervisorAnalystRevenue = 0m,
                commissionPayingProjectId = (Guid?)null,
                payload = new
                {
                    projectEntries = Array.Empty<object>(),
                    rateioProjectEntries = Array.Empty<object>(),
                    commercialProjectEntries = Array.Empty<object>(),
                    supervisorProjectEntries = Array.Empty<object>(),
                    trafficProjectEntries = Array.Empty<object>(),
                    managementRevenueEntries = Array.Empty<object>(),
                    bonusEntries = Array.Empty<object>(),
                    deductionEntries = Array.Empty<object>(),
                    complementPayingProjects = Array.Empty<object>(),
                    roleChanges = Array.Empty<object>()
                },
                result = new
                {
                    totalAmount = 4300m,
                    baseSalary = 870m,
                    commissionAmount = 3430m,
                    goalBonusAmount = 0m,
                    groupCommissionAmount = 0m,
                    platformTotal = 400m
                },
                projectTotals = new[]
                {
                    new { projectId = ProjectId, amount = 3900m }
                }
            }
            : null;

        return JsonSerializer.Serialize(new
        {
            id = PayrollId,
            departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            departmentName = "Analistas Comerciais",
            month = 1,
            year = 2026,
            status,
            totalAmount = 4300m,
            entryCount = includeEntry ? 1 : 0,
            submittedBy = "Maria Manager",
            rejectionComment,
            entries = entry is null ? Array.Empty<object>() : new[] { entry },
            editorOptions = new
            {
                projects = new[]
                {
                    new
                    {
                        id = ProjectId,
                        name = "Projeto Demo",
                        platform = "lastlink",
                        excludesGoalBonus = false,
                        isDefaultAllocationTarget = false,
                        excludesSupervisorFixedAllocation = false
                    }
                },
                departments = Array.Empty<object>(),
                careerLevels = Array.Empty<object>()
            },
            allowedActions = allowedActions ?? PayrollAllowedActionsTestHelper.ForManagerDraft()
        });
    }

    private Task AuthenticateWithPayrollReadAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

    private Task AuthenticateWithPayrollWriteAsync() =>
        AuthenticateWithPayrollReadAsync();
}
