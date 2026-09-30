using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Ui;
using WebApp.Blazor.Pages.Dev;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests;

public class ThemeTests : BlazorComponentTestContext
{

    [Fact]
    public void TokensPage_RendersAllSampleSwatches()
    {
        var cut = Render<Tokens>();

        cut.Find("[data-token='background']").Should().NotBeNull();
        cut.Find("[data-token='card']").Should().NotBeNull();
        cut.Find("[data-token='primary']").Should().NotBeNull();
        cut.Find("[data-token='destructive']").Should().NotBeNull();
        cut.Find("[data-token='sidebar']").Should().NotBeNull();
    }

    [Fact]
    public void ThemeToggle_RendersAccessibleLabel()
    {
        var cut = Render<ThemeToggle>();

        var button = cut.Find("button.theme-toggle");
        button.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ThemeToggle_TogglesThemeOnClick()
    {
        JSInterop.Setup<string>("corepayTheme.get").SetResult("dark");
        JSInterop.SetupVoid("corepayTheme.apply");

        var cut = Render<ThemeToggle>();

        cut.Find("button.theme-toggle").Click();

        JSInterop.VerifyInvoke("corepayTheme.apply");
    }

    [Fact]
    public async Task ThemeService_DefaultsToDarkWhenStorageEmpty()
    {
        JSInterop.Setup<string>("corepayTheme.get").SetResult(string.Empty);

        var service = Services.GetRequiredService<ThemeService>();
        await service.InitializeAsync();

        service.IsDark.Should().BeTrue();
        service.CurrentTheme.Should().Be("dark");
    }

    [Fact]
    public async Task ThemeService_RestoresLightFromStorage()
    {
        JSInterop.Setup<string>("corepayTheme.get").SetResult("light");

        var service = Services.GetRequiredService<ThemeService>();
        await service.InitializeAsync();

        service.IsDark.Should().BeFalse();
        service.CurrentTheme.Should().Be("light");
    }

    [Fact]
    public async Task ThemeService_ToggleSwitchesThemeAndPersists()
    {
        JSInterop.Setup<string>("corepayTheme.get").SetResult("dark");
        JSInterop.SetupVoid("corepayTheme.apply");

        var service = Services.GetRequiredService<ThemeService>();
        await service.InitializeAsync();

        await service.ToggleAsync();

        service.IsDark.Should().BeFalse();
        service.CurrentTheme.Should().Be("light");
        JSInterop.VerifyInvoke("corepayTheme.apply");
    }
}
