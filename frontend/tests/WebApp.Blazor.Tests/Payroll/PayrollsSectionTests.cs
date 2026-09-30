using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Payroll;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public PayrollsSectionTests()
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
        Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
    }

    [Fact]
    public async Task PayrollsSection_Admin_ShowsGroupedPayrollsAndDuplicateButton()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreatePayrollListJson(), CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Jan/2026");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("R$ 12.500,00");
            cut.Markup.Should().Contain("Paga");
            cut.Markup.Should().Contain("Duplicar");
            cut.Find(".payrolls-competence-header").Should().NotBeNull();
            cut.Find(".status-badge").Should().NotBeNull();
            cut.Find(".text-table-money").Should().NotBeNull();
        });
    }

    [Fact]
    public async Task PayrollsSection_Manager_HidesDepartmentFilter()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreatePayrollListJson());
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().NotContain("Todos os setores");
        });
    }

    [Fact]
    public async Task PayrollsSection_Director_DoesNotShowDuplicateButton()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreatePayrollListJson(), CreateDepartmentListJson());
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().NotContain("Duplicar");
        });
    }

    [Fact]
    public async Task PayrollsSection_EmptyList_ShowsEmptyState()
    {
        ConfigureApiResponses(HttpStatusCode.OK, CreateEmptyPayrollListJson(), CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nenhuma folha encontrada");
        });
    }

    [Fact]
    public async Task PayrollsSection_LoadError_ShowsEmptyStateWithError()
    {
        ConfigureApiResponses(HttpStatusCode.InternalServerError, null, CreateDepartmentListJson());
        await AuthenticateAsAdminAsync();

        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Erro ao carregar folhas de pagamento");
        });
    }

    [Fact]
    public async Task PayrollsSection_StatusFilter_RequestsFilteredQuery()
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

            capturedPath = request.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateEmptyPayrollListJson())
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() => cut.FindAll("select").Count.Should().BeGreaterThanOrEqualTo(3));
        cut.FindAll("select")[2].Change("paid");

        cut.WaitForAssertion(() =>
        {
            capturedPath.Should().Contain("status=paid");
        });
    }

    [Fact]
    public async Task PayrollsSection_Duplicate_Conflict_ShowsMessage()
    {
        var payrollId = Guid.Parse("33333333-3333-3333-3333-333333333333");
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
                && request.RequestUri!.AbsolutePath == "/api/v1/payrolls")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreatePayrollListJson(payrollId))
                };
            }

            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{payrollId}/duplicate");
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("""{"error":"payrolls.competence_duplicate","message":"Duplicate."}""")
            };
        });

        await AuthenticateAsAdminAsync();
        var cut = Render<PayrollsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Duplicar"));
        cut.FindAll("button").First(button => button.TextContent?.Contains("Duplicar") == true).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Já existe folha para este setor na competência seguinte.");
        });
    }

    private void ConfigureApiResponses(
        HttpStatusCode payrollsStatus,
        string? payrollsBody,
        string? departmentsBody = null)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/departments")
            {
                return departmentsBody is null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(departmentsBody)
                    };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/payrolls")
            {
                return payrollsBody is null
                    ? new HttpResponseMessage(payrollsStatus)
                    : new HttpResponseMessage(payrollsStatus)
                    {
                        Content = new StringContent(payrollsBody)
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

    private static string CreateEmptyPayrollListJson() =>
        JsonSerializer.Serialize(new
        {
            items = Array.Empty<object>(),
            totalCount = 0,
            page = 1,
            pageSize = 30
        });

    private static string CreatePayrollListJson(Guid? id = null) =>
        JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    id = id ?? Guid.NewGuid(),
                    departmentId = Guid.NewGuid(),
                    departmentName = "Analistas Comerciais",
                    month = 1,
                    year = 2026,
                    status = "paid",
                    totalAmount = 12_500m,
                    entryCount = 2,
                    submittedBy = "manager@test"
                }
            },
            totalCount = 1,
            page = 1,
            pageSize = 30
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
