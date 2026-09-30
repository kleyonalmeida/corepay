using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Layout;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Layout;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Layout;

public class ShellLayoutTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public ShellLayoutTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<SidebarState>();
        Services.AddScoped<AuthService>();
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<INotificationApiService, NotificationApiService>();
        Services.AddScoped<NotificationState>();
    }

    [Fact]
    public async Task MainLayout_Unauthenticated_RendersBodyWithoutShell()
    {
        var authService = Services.GetRequiredService<AuthService>();
        await authService.InitializeAsync();

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "conteudo"))));

        cut.Markup.Should().Contain("conteudo");
        cut.Markup.Should().NotContain("app-sidebar");
        cut.Markup.Should().NotContain("app-header");
    }

    [Fact]
    public async Task MainLayout_Authenticated_RendersShellWithBrandAndUser()
    {
        await AuthenticateAsync("Super Admin");

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "dashboard"))));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("app-sidebar");
            cut.Markup.Should().Contain("app-header");
            cut.Markup.Should().Contain("CorePay");
            cut.Markup.Should().Contain("Gestão de Pagamentos");
            cut.Find(".app-sidebar__logo-mark").Should().NotBeNull();
            cut.Find(".text-brand").Should().NotBeNull();
            cut.Markup.Should().Contain("Super Admin");
            cut.Markup.Should().Contain("dashboard");
        });
    }

    [Fact]
    public async Task AppSidebar_RendersAllNavigationItems()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Dashboard");
            cut.Markup.Should().Contain(ShellNavigation.PayrollsDisplayName);
            cut.Markup.Should().Contain("Colaboradores");
            cut.Markup.Should().Contain("Faturamento");
            cut.Markup.Should().Contain("Investimento Tráfego");
            cut.Markup.Should().Contain("Financeiro");
            cut.Markup.Should().Contain("Fluxo de Caixa");
            cut.Markup.Should().Contain("Relatórios");
            cut.Markup.Should().Contain("Configurações");
            cut.Markup.Should().Contain("Usuários");
            cut.Markup.Should().Contain("Papéis");
        });
    }

    [Fact]
    public async Task AppSidebar_ActiveRoute_ShowsActiveClassAndChevron()
    {
        await AuthenticateAsync("Usuário Teste");

        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo("/payrolls");

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            var activeLink = cut.Find("a.app-sidebar__nav-link--active[href='/payrolls']");
            activeLink.Should().NotBeNull();
            activeLink.QuerySelector(".app-sidebar__nav-chevron").Should().BeNull();
        });
    }

    [Fact]
    public async Task AppHeader_RendersThemeToggleAndNotifications()
    {
        ConfigureNotificationsResponse("""
            { "items": [], "unreadCount": 0 }
            """);
        await AuthenticateAsync("Usuário Teste");

        var cut = Render<AppHeader>();

        cut.Find(".theme-toggle").Should().NotBeNull();
        cut.Find("a.app-header__notifications").Should().NotBeNull();
    }

    [Fact]
    public async Task AppHeader_WithUnreadCount_ShowsDestructiveBadge()
    {
        ConfigureNotificationsResponse("""
            {
              "items": [
                {
                  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "type": "payroll_submitted",
                  "title": "Folha submetida",
                  "message": "Teste",
                  "payrollId": "11111111-1111-1111-1111-111111111111",
                  "isRead": false,
                  "createdAt": "2026-03-10T15:00:00+00:00"
                }
              ],
              "unreadCount": 3
            }
            """);
        await AuthenticateAsync("Usuário Teste");

        var cut = Render<AppHeader>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".app-header__notifications-badge").TextContent.Should().Contain("3");
        });
    }

    [Fact]
    public async Task AppHeader_ZeroUnread_HidesBadge()
    {
        ConfigureNotificationsResponse("""
            { "items": [], "unreadCount": 0 }
            """);
        await AuthenticateAsync("Usuário Teste");

        var cut = Render<AppHeader>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("app-header__notifications-badge");
        });
    }

    [Fact]
    public async Task AppHeader_ShowsRouteTitleForCurrentPath()
    {
        await AuthenticateAsync("Usuário Teste");

        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo("/financial");

        var cut = Render<AppHeader>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".app-header__title").TextContent.Should().Contain("Financeiro");
        });
    }

    [Fact]
    public async Task SidebarTrigger_DesktopToggle_CollapsesSidebar()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        var cut = Render<SidebarTrigger>();

        cut.Find(".sidebar-trigger--desktop").Click();

        sidebarState.IsDesktopCollapsed.Should().BeTrue();
    }

    [Fact]
    public async Task SidebarTrigger_MobileToggle_SwitchesIconWhenOpen()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        var cut = Render<SidebarTrigger>();

        cut.Find(".sidebar-trigger--mobile").Click();

        cut.WaitForAssertion(() =>
        {
            sidebarState.IsMobileOpen.Should().BeTrue();
            cut.Find(".sidebar-trigger--mobile").GetAttribute("aria-label").Should().Be("Fechar menu");
        });
    }

    [Fact]
    public async Task AppSidebar_EveryNavItem_HasIcon()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All);

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll(".app-sidebar__nav-link");
            links.Should().NotBeEmpty();
            foreach (var link in links)
            {
                link.QuerySelector(".app-sidebar__nav-icon").Should().NotBeNull(
                    because: $"cada item de nav deve ter ícone: {link.TextContent}");
            }
        });
    }

    [Fact]
    public async Task SidebarTrigger_MobileToggle_OpensDrawer()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        var cut = Render<SidebarTrigger>();

        cut.Find(".sidebar-trigger--mobile").Click();

        sidebarState.IsMobileOpen.Should().BeTrue();
    }

    [Fact]
    public async Task MainLayout_MobileOpen_RendersOverlay()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        sidebarState.ToggleMobileDrawer();

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        cut.Find(".app-shell__overlay").Should().NotBeNull();
    }

    [Fact]
    public async Task MainLayout_OverlayClick_ClosesMobileDrawer()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        sidebarState.ToggleMobileDrawer();

        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "body"))));

        cut.Find(".app-shell__overlay").Click();

        sidebarState.IsMobileOpen.Should().BeFalse();
    }

    [Fact]
    public async Task AppSidebar_Navigation_ClosesMobileDrawer()
    {
        await AuthenticateAsync("Usuário Teste");

        var sidebarState = Services.GetRequiredService<SidebarState>();
        sidebarState.ToggleMobileDrawer();

        var cut = Render<AppSidebar>();
        cut.Find("a[href='/payrolls']").Click();

        sidebarState.IsMobileOpen.Should().BeFalse();
    }

    private Task AuthenticateAsync(string displayName) =>
        AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All,
            displayName);

    private void ConfigureNotificationsResponse(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/notifications")
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
    }
}
