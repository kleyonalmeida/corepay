using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Layout;
using WebApp.Blazor.Layout;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Layout;

public class PayrollShellNavigationTests : BlazorComponentTestContext
{
    public PayrollShellNavigationTests()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<SidebarState>();
        Services.AddScoped<AuthService>();
        Services.AddScoped<INotificationApiService, NotificationApiService>();
        Services.AddScoped<NotificationState>();
        Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
    }

    [Theory]
    [InlineData("/payrolls/new")]
    [InlineData("/payrolls/3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData("/payrolls/3fa85f64-5717-4562-b3fc-2c963f66afa6/edit")]
    public async Task AppHeader_ShowsFolhasTitleForPayrollSubroutes(string path)
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All);

        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo(path);

        var cut = Render<AppHeader>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".app-header__title").TextContent.Should().Contain(ShellNavigation.PayrollsDisplayName);
        });
    }

    [Fact]
    public async Task AppSidebar_PayrollSubroute_KeepsPayrollsNavActive()
    {
        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All);

        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo("/payrolls/new");

        var cut = Render<AppSidebar>();

        cut.WaitForAssertion(() =>
        {
            var activeLink = cut.Find("a.app-sidebar__nav-link--active[href='/payrolls']");
            activeLink.Should().NotBeNull();
        });
    }
}
