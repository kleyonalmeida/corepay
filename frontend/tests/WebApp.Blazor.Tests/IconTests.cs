using FluentAssertions;
using WebApp.Blazor.Components.Ui;

namespace WebApp.Blazor.Tests;

public class IconTests : BlazorComponentTestContext
{

    [Fact]
    public void Icon_RendersLucideSvgWithLinearStroke()
    {
        var cut = Render<Icon>(p => p.Add(x => x.Kind, IconKind.Plus));

        var svg = cut.Find("svg");
        svg.GetAttribute("fill").Should().Be("none");
        svg.GetAttribute("stroke").Should().Be("currentColor");
        svg.GetAttribute("stroke-linecap").Should().Be("round");
        svg.GetAttribute("stroke-linejoin").Should().Be("round");
    }

    [Theory]
    [InlineData(IconSize.Default, 16, "ui-icon--default")]
    [InlineData(IconSize.Topbar, 20, "ui-icon--topbar")]
    [InlineData(IconSize.AuthHero, 28, "ui-icon--auth-hero")]
    [InlineData(IconSize.EmptyState, 32, "ui-icon--empty-state")]
    public void Icon_RendersSemanticSize(IconSize size, int expectedPixels, string expectedClass)
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Kind, IconKind.Bell)
            .Add(x => x.Size, size));

        var wrapper = cut.Find(".ui-icon");
        wrapper.ClassList.Should().Contain(expectedClass);

        var svg = cut.Find("svg");
        svg.GetAttribute("width").Should().Be(expectedPixels.ToString());
        svg.GetAttribute("height").Should().Be(expectedPixels.ToString());
    }

    [Fact]
    public void Icon_Decorative_IsAriaHidden()
    {
        var cut = Render<Icon>(p => p
            .Add(x => x.Kind, IconKind.Search)
            .Add(x => x.AriaHidden, true));

        cut.Find(".ui-icon").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Theory]
    [InlineData(IconKind.LayoutDashboard, "layout-dashboard")]
    [InlineData(IconKind.FileText, "file-text")]
    [InlineData(IconKind.PanelLeftClose, "panel-left-close")]
    [InlineData(IconKind.CheckCircle2, "circle-check")]
    public void IconMetadata_MapsCatalogNames(IconKind kind, string expectedLucideName)
    {
        IconMetadata.GetLucideName(kind).Should().Be(expectedLucideName);
    }

    [Fact]
    public void IconMetadata_CoversAllCatalogKinds()
    {
        var kinds = Enum.GetValues<IconKind>();
        kinds.Should().NotBeEmpty();

        foreach (var kind in kinds)
        {
            var act = () => IconMetadata.GetLucideName(kind);
            act.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(IconKind.BarChart3)]
    [InlineData(IconKind.ChartColumn)]
    [InlineData(IconKind.CheckCircle2)]
    [InlineData(IconKind.Wallet)]
    [InlineData(IconKind.Eye)]
    [InlineData(IconKind.EyeOff)]
    public void Icon_RendersKnownSvgPath(IconKind kind)
    {
        var cut = Render<Icon>(p => p.Add(x => x.Kind, kind));

        cut.FindAll("svg path").Should().NotBeEmpty(
            because: $"{kind} deve resolver para um ícone Lucide válido");
    }
}
