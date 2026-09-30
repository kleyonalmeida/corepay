using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Layout;
using WebApp.Blazor.Components.Ui;
using WebApp.Blazor.Formatting;
using WebApp.Blazor.Pages;
using WebApp.Blazor.Pages.Dev;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.VisualParity;

/// <summary>
/// Checklist §14 de IDENTIDADE_VISUAL.md — marcadores de markup/classes por tela (Fase 15.3).
/// Cores computadas e breakpoints reais exigem QA manual documentado em docs/IDENTIDADE_VISUAL.md §15.3.
/// </summary>
public class VisualParityChecklistTests : BlazorComponentTestContext
{
    [Fact]
    public void DevBaselines_ExposeTokenTypographyAndKitchenMarkers()
    {
        var tokens = Render<Tokens>();
        tokens.Find("[data-token='background']").Should().NotBeNull();
        tokens.Find("[data-token='primary']").Should().NotBeNull();

        var typography = Render<Typography>();
        typography.Find(".text-page-title").Should().NotBeNull();
        typography.Find(".text-table-money").Should().NotBeNull();
        typography.Find(".tabular-nums").Should().NotBeNull();

        var kitchen = Render<Kitchen>();
        kitchen.Find("[data-kitchen='stat-card']").Should().NotBeNull();
        kitchen.Find("[data-kitchen='status-badge']").Should().NotBeNull();
        kitchen.Find(".ui-button--default").Should().NotBeNull();
    }

    [Fact]
    public void Login_MarksCorePayBrandAndAuthSurface()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        Services.AddScoped<AuthorizationMessageHandler>();
        var loginHttpHandler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        Services.AddScoped(_ => new HttpClient(loginHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<AuthService>();

        var cut = Render<Login>();

        cut.Find(".auth-page").Should().NotBeNull();
        cut.Find(".auth-page__icon-wrap").Should().NotBeNull();
        cut.Find(".auth-card").Should().NotBeNull();
        cut.Find(".text-auth-title").TextContent.Should().Contain("CorePay");
        cut.Find("button.auth-submit").ClassList.Should().Contain("ui-button--default");
    }

    [Fact]
    public async Task Shell_SidebarMarksBrandLogoAndActiveNav()
    {
        Services.AddSingleton<IAuthSessionStorage>(new InMemoryAuthSessionStorage());
        Services.AddScoped<CorePayAuthenticationStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        AuthorizationTestSetup.AddCorePayAuthorization(Services);
        Services.AddScoped<SidebarState>();
        Services.AddScoped<AuthService>();

        await AuthTestHelper.AuthenticateAsync(
            Services,
            [AppRoles.SuperAdmin],
            AppPermissions.All);

        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo("/financial");

        var cut = Render<AppSidebar>();

        cut.Find(".app-sidebar__logo-mark").Should().NotBeNull();
        cut.Find(".text-brand").TextContent.Should().Contain("CorePay");
        cut.Find("a.app-sidebar__nav-link--active[href='/financial']").Should().NotBeNull();
    }

    [Fact]
    public void StatCard_SemanticTones_MatchTrafficPalette()
    {
        var tones = new (SemanticTone Tone, string ClassSuffix)[]
        {
            (SemanticTone.Yellow, "yellow"),
            (SemanticTone.Emerald, "emerald"),
            (SemanticTone.Purple, "purple"),
            (SemanticTone.Orange, "orange")
        };

        foreach (var (tone, suffix) in tones)
        {
            var cut = Render<StatCard>(p => p
                .Add(x => x.Label, tone.ToString())
                .Add(x => x.Value, 100m)
                .Add(x => x.Tone, tone));

            cut.Find($".stat-card__icon--{suffix}").Should().NotBeNull();
        }
    }

    [Fact]
    public void StatusBadge_RendersPillNotPlainText()
    {
        var cut = Render<StatusBadge>(p => p.Add(x => x.Kind, StatusKind.Approved));

        var badge = cut.Find(".status-badge");
        badge.ClassList.Should().Contain("status-badge--pill");
        badge.ClassList.Should().Contain("status-badge--approved");
    }

    [Fact]
    public void MoneyFormatter_UsesTabularPtBr()
    {
        MoneyFormatter.FormatMoney(1234.56m).Should().Be("R$ 1.234,56");
    }
}
