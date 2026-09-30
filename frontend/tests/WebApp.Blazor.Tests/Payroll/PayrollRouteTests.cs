using System.Net;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Layout;
using WebApp.Blazor.Pages;
using WebApp.Blazor.Pages.Payroll;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollRouteTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingPayrollId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    public PayrollRouteTests()
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
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
    }

    public static TheoryData<Type, string?> PagePolicyData => new()
    {
        { typeof(Payrolls), AppPolicies.PayrollsRead },
        { typeof(NewPayroll), AppPolicies.PayrollsWrite },
        { typeof(PayrollDetail), AppPolicies.PayrollsRead },
        { typeof(PayrollEdit), AppPolicies.PayrollsWrite }
    };

    [Theory]
    [MemberData(nameof(PagePolicyData))]
    public void Page_HasExpectedAuthorizePolicy(Type pageType, string? expectedPolicy)
    {
        var authorize = pageType.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull($"{pageType.Name} must require authorization");
        authorize!.Policy.Should().Be(expectedPolicy);
    }

    [Theory]
    [InlineData("/payrolls")]
    [InlineData("/payrolls/new")]
    [InlineData("/payrolls/3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData("/payrolls/3fa85f64-5717-4562-b3fc-2c963f66afa6/edit")]
    public void ShellNavigation_ResolvesPayrollSubroutesToFolhasTitle(string path)
    {
        ShellNavigation.GetTitleForPath(path).Should().Be(ShellNavigation.PayrollsDisplayName);
    }

    [Fact]
    public async Task PayrollDetail_NotFound_ShowsEmptyState()
    {
        ConfigurePayrollResponse(HttpStatusCode.NotFound);
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Folha não encontrada");
            cut.Markup.Should().NotContain("Acesso negado");
        });
    }

    [Fact]
    public async Task PayrollDetail_Forbidden_ShowsAccessDenied()
    {
        ConfigurePayrollResponse(HttpStatusCode.Forbidden);
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Acesso negado");
            cut.Markup.Should().NotContain("Folha não encontrada");
        });
    }

    [Fact]
    public async Task PayrollDetail_Success_ShowsPayrollContent()
    {
        ConfigurePayrollResponse(HttpStatusCode.OK, CreatePayrollJson("draft"));
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<PayrollDetail>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Detalhe da folha");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().NotContain("Folha não encontrada");
            cut.Markup.Should().NotContain("Acesso negado");
        });
    }

    [Fact]
    public async Task PayrollEdit_NotFound_ShowsEmptyState()
    {
        ConfigurePayrollResponse(HttpStatusCode.NotFound);
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollEdit>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Folha não encontrada");
        });
    }

    [Fact]
    public async Task PayrollEdit_Forbidden_ShowsAccessDenied()
    {
        ConfigurePayrollResponse(HttpStatusCode.Forbidden);
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollEdit>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Acesso negado");
        });
    }

    [Fact]
    public async Task PayrollEdit_Success_ShowsEditContent()
    {
        ConfigurePayrollResponses(HttpStatusCode.OK, CreatePayrollJson("draft"));
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollEdit>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Editar folha");
            cut.Markup.Should().Contain("Analistas Comerciais");
            cut.Markup.Should().Contain("Competência");
            cut.Markup.Should().NotContain("Em breve");
        });
    }

    [Fact]
    public async Task PayrollEdit_PendingApproval_ShowsNotEditableState()
    {
        ConfigurePayrollResponses(HttpStatusCode.OK, CreatePayrollJson("pendingApproval"));
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<PayrollEdit>(parameters => parameters
            .Add(p => p.Id, ExistingPayrollId));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Folha não editável");
            cut.Markup.Should().NotContain("Adicionar colaborador");
        });
    }

    [Fact]
    public async Task NewPayroll_RequiresWritePermission()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);

        var authorizationService = Services.GetRequiredService<IAuthorizationService>();
        var authState = await Services
            .GetRequiredService<AuthenticationStateProvider>()
            .GetAuthenticationStateAsync();

        var result = await authorizationService.AuthorizeAsync(authState.User, AppPolicies.PayrollsWrite);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task NewPayroll_ManagerWithWrite_CanRender()
    {
        ConfigureFormOptionsResponse();
        await AuthenticateWithPayrollWriteAsync();

        var cut = Render<NewPayroll>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nova folha");
            cut.Markup.Should().Contain("Competência");
            cut.Markup.Should().NotContain("Em breve");
        });
    }

    [Fact]
    public async Task NotFound_Authenticated_RendersPortugueseMessageInShell()
    {
        await AuthenticateWithPayrollReadAsync();

        var cut = Render<NotFound>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Página não encontrada");
            cut.Markup.Should().NotContain("Sorry, the content");
        });
    }

    private void ConfigurePayrollResponse(HttpStatusCode status, string? jsonBody = null)
    {
        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/auth/me")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "id": "user-id",
                          "email": "user@test.com",
                          "displayName": "Usuário Teste",
                          "roles": ["User"],
                          "permissions": ["payrolls.read", "payrolls.write"],
                          "departmentIds": []
                        }
                        """)
                };
            }

            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/payrolls/{ExistingPayrollId}");
            request.Method.Should().Be(HttpMethod.Get);

            if (status == HttpStatusCode.OK)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonBody ?? CreatePayrollJson("draft"))
                };
            }

            return new HttpResponseMessage(status);
        });
    }

    private void ConfigurePayrollResponses(HttpStatusCode status, string payrollJson)
    {
        _httpHandler.Configure(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/payrolls/form-options")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateFormOptionsJson())
                };
            }

            if (path == $"/api/v1/payrolls/{ExistingPayrollId}" && request.Method == HttpMethod.Get)
            {
                return status == HttpStatusCode.OK
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payrollJson) }
                    : new HttpResponseMessage(status);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureFormOptionsResponse()
    {
        _httpHandler.Configure(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/payrolls/form-options")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateFormOptionsJson())
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private static string CreatePayrollJson(string status) =>
        JsonSerializer.Serialize(new
        {
            id = ExistingPayrollId,
            departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            departmentName = "Analistas Comerciais",
            month = 1,
            year = 2026,
            status,
            totalAmount = 0m,
            entryCount = 0,
            submittedBy = (string?)null,
            rejectionComment = (string?)null,
            entries = Array.Empty<object>(),
            editorOptions = new
            {
                projects = Array.Empty<object>(),
                departments = Array.Empty<object>(),
                careerLevels = Array.Empty<object>()
            },
            allowedActions = PayrollAllowedActionsTestHelper.ForManagerDraft()
        });

    private static string CreateFormOptionsJson() =>
        JsonSerializer.Serialize(new
        {
            departments = new[]
            {
                new
                {
                    id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    name = "Analistas Comerciais"
                }
            }
        });

    private Task AuthenticateWithPayrollReadAsync() =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

    private Task AuthenticateWithPayrollWriteAsync() =>
        AuthenticateWithPayrollReadAsync();
}
