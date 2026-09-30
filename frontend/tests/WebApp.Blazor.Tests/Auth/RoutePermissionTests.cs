using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Pages;
using WebApp.Blazor.Pages.Admin;
using WebApp.Blazor.Pages.Payroll;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

public class RoutePermissionTests : BlazorComponentTestContext
{
    public RoutePermissionTests()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<AuthService>();
        Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
    }

    public static TheoryData<Type, string?> PagePolicyData => new()
    {
        { typeof(Home), null },
        { typeof(Payrolls), AppPolicies.PayrollsRead },
        { typeof(NewPayroll), AppPolicies.PayrollsWrite },
        { typeof(PayrollDetail), AppPolicies.PayrollsRead },
        { typeof(PayrollEdit), AppPolicies.PayrollsWrite },
        { typeof(WebApp.Blazor.Pages.Collaborators), AppPolicies.CollaboratorsRead },
        { typeof(WebApp.Blazor.Pages.ProjectRevenues), AppPolicies.RevenuesRead },
        { typeof(WebApp.Blazor.Pages.AnalystMetrics), AppPolicies.AnalystMetricsRead },
        { typeof(TrafficInvestment), AppPolicies.TrafficRead },
        { typeof(WebApp.Blazor.Pages.Financial), AppPolicies.FinanceRead },
        { typeof(CashFlow), AppPolicies.CashflowRead },
        { typeof(WebApp.Blazor.Pages.Reports), AppPolicies.ReportsRead },
        { typeof(WebApp.Blazor.Pages.Settings), AppPolicies.MasterData },
        { typeof(WebApp.Blazor.Pages.Notifications), null },
        { typeof(Users), AppPolicies.UsersRead },
        { typeof(Roles), AppPolicies.RolesRead }
    };

    [Theory]
    [MemberData(nameof(PagePolicyData))]
    public void Page_HasExpectedAuthorizePolicy(Type pageType, string? expectedPolicy)
    {
        var authorize = pageType.GetCustomAttribute<AuthorizeAttribute>();
        authorize.Should().NotBeNull($"{pageType.Name} must require authorization");

        if (expectedPolicy is null)
        {
            authorize!.Policy.Should().BeNullOrEmpty();
        }
        else
        {
            authorize!.Policy.Should().Be(expectedPolicy);
        }
    }

    [Fact]
    public async Task Manager_CannotAuthorizeFinancialPolicy()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var authorizationService = Services.GetRequiredService<IAuthorizationService>();
        var authState = await Services
            .GetRequiredService<AuthenticationStateProvider>()
            .GetAuthenticationStateAsync();

        var result = await authorizationService.AuthorizeAsync(authState.User, AppPolicies.FinanceRead);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Manager_AccessingFinancialRoute_ShowsAccessDenied()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<AuthorizeView>(viewParams => viewParams
                .Add(v => v.Policy, AppPolicies.FinanceRead)
                .Add(v => v.Authorized, (RenderFragment<AuthenticationState>)(_ =>
                    builder => builder.AddContent(0, "Financeiro")))
                .Add(v => v.NotAuthorized, (RenderFragment<AuthenticationState>)(_ =>
                    builder => builder.AddContent(0, "Acesso negado")))));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Acesso negado");
            cut.Markup.Should().NotContain("Financeiro");
        });
    }
}
