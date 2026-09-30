using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Layout;
using WebApp.Blazor.Layout;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Layout;

public class SidebarPermissionTests : BlazorComponentTestContext
{
    public SidebarPermissionTests()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<SidebarState>();
        Services.AddScoped<AuthService>();
        Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
    }

    [Fact]
    public async Task SuperAdmin_SeesAllNavigationItems()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            []);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            AssertContainsAllMainAndAdminItems(cut.Markup);
        });
    }

    [Fact]
    public async Task Admin_SeesAllNavigationItems()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Admin"],
            ReferenceRolePermissions.Map["Admin"]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            AssertContainsAllMainAndAdminItems(cut.Markup);
        });
    }

    [Fact]
    public async Task Manager_SeesOnlyOperationalMenuWithoutFinanceReportsOrCashflow()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Manager"],
            ReferenceRolePermissions.Map["Manager"]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Dashboard");
            cut.Markup.Should().Contain(ShellNavigation.PayrollsDisplayName);
            cut.Markup.Should().Contain("Colaboradores");
            cut.Markup.Should().Contain("Métricas de analista");
            cut.Markup.Should().Contain("Faturamento");
            cut.Markup.Should().NotContain("Investimento Tráfego");
            cut.Markup.Should().NotContain("Financeiro");
            cut.Markup.Should().NotContain("Fluxo de Caixa");
            cut.Markup.Should().NotContain("Relatórios");
            cut.Markup.Should().NotContain("Configurações");
            cut.Markup.Should().NotContain("Administração");
            cut.Markup.Should().NotContain("Usuários");
            cut.Markup.Should().NotContain("Papéis");
        });
    }

    [Fact]
    public async Task Director_SeesSettingsWithoutAdminSection()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Director"],
            ReferenceRolePermissions.Map["Director"]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Configurações");
            cut.Markup.Should().Contain("Financeiro");
            cut.Markup.Should().NotContain("Administração");
            cut.Markup.Should().NotContain("Usuários");
            cut.Markup.Should().NotContain("Papéis");
        });
    }

    [Fact]
    public async Task Financial_DoesNotSeeTrafficOrSettings()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["Financial"],
            ReferenceRolePermissions.Map["Financial"]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Financeiro");
            cut.Markup.Should().Contain("Relatórios");
            cut.Markup.Should().NotContain("Investimento Tráfego");
            cut.Markup.Should().NotContain("Configurações");
            cut.Markup.Should().NotContain("Administração");
        });
    }

    [Fact]
    public async Task User_WithNoPermissions_SeesOnlyDashboard()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["User"],
            ReferenceRolePermissions.Map["User"]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Dashboard");
            cut.Markup.Should().NotContain(ShellNavigation.PayrollsDisplayName);
            cut.Markup.Should().NotContain("Colaboradores");
            cut.Markup.Should().NotContain("Administração");
        });
    }

    [Fact]
    public async Task TrafficReader_SeesTrafficWithoutBeingAdmin()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            ["CustomTraffic"],
            [AppPermissions.TrafficRead]);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Dashboard");
            cut.Markup.Should().Contain("Investimento Tráfego");
            cut.Markup.Should().NotContain("Financeiro");
            cut.Markup.Should().NotContain("Usuários");
        });
    }

    private static void AssertContainsAllMainAndAdminItems(string markup)
    {
        markup.Should().Contain("Dashboard");
        markup.Should().Contain(ShellNavigation.PayrollsDisplayName);
        markup.Should().Contain("Colaboradores");
        markup.Should().Contain("Métricas de analista");
        markup.Should().Contain("Faturamento");
        markup.Should().Contain("Investimento Tráfego");
        markup.Should().Contain("Financeiro");
        markup.Should().Contain("Fluxo de Caixa");
        markup.Should().Contain("Relatórios");
        markup.Should().Contain("Configurações");
        markup.Should().Contain("Administração");
        markup.Should().Contain("Usuários");
        markup.Should().Contain("Papéis");
    }
}
