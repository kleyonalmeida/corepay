using FluentAssertions;
using WebApp.Blazor.Pages.Dev;

namespace WebApp.Blazor.Tests;

public class TypographyTests : BlazorComponentTestContext
{

    [Fact]
    public void TypographyPage_RendersPageTitleWithCorrectClass()
    {
        var cut = Render<Typography>();

        var title = cut.Find("[data-typography='page-title']");
        title.ClassList.Should().Contain("text-page-title");
        title.TextContent.Should().Contain("Folha Jan/2026");
    }

    [Fact]
    public void TypographyPage_RendersLabelWithCorrectClass()
    {
        var cut = Render<Typography>();

        var label = cut.Find("[data-typography='label']");
        label.ClassList.Should().Contain("text-label");
        label.TextContent.Should().Contain("Total a pagar");
    }

    [Fact]
    public void TypographyPage_RendersStatValueWithTabularNumsAndFormattedMoney()
    {
        var cut = Render<Typography>();

        var value = cut.Find("[data-typography='stat-value']");
        value.ClassList.Should().Contain("text-stat-value");
        value.ClassList.Should().Contain("tabular-nums");
        value.TextContent.Should().Be("R$ 1.234,56");
    }
}
