using FluentAssertions;
using WebApp.Blazor.Pages.Dev;

namespace WebApp.Blazor.Tests;

public class KitchenTests : BlazorComponentTestContext
{
    private static readonly string[] ExpectedSections =
    [
        "button",
        "status-badge",
        "stat-card",
        "card",
        "input",
        "select",
        "tabs",
        "dialog",
        "goal-toggle",
        "avatar",
        "spinner",
        "empty-state",
        "icons"
    ];

    [Fact]
    public void KitchenPage_RendersAllTwelveSections()
    {
        var cut = Render<Kitchen>();

        foreach (var section in ExpectedSections)
        {
            cut.Find($"[data-kitchen='{section}']").Should().NotBeNull($"section {section} should exist");
        }
    }

    [Fact]
    public void KitchenPage_RendersAllStatusBadges()
    {
        var cut = Render<Kitchen>();

        cut.FindAll(".status-badge").Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void KitchenPage_ButtonIsNotPill_StatusBadgeIsPill()
    {
        var cut = Render<Kitchen>();

        var button = cut.Find("[data-kitchen='button'] .ui-button--default");
        button.ClassList.Should().NotContain("status-badge--pill");
        button.ClassList.Should().NotContain("ui-button--pill");

        var badge = cut.Find("[data-kitchen='status-badge'] .status-badge");
        badge.ClassList.Should().Contain("status-badge--pill");
    }

    [Fact]
    public void KitchenPage_GoalToggleCheckedUsesEmerald()
    {
        var cut = Render<Kitchen>();

        var toggle = cut.Find("[data-kitchen='goal-toggle'] .goal-toggle--checked");
        toggle.ClassList.Should().Contain("goal-toggle--checked");
        toggle.ClassList.Should().NotContain("goal-toggle--primary");
    }

    [Fact]
    public void KitchenPage_IconButtonUsesLucidePlus()
    {
        var cut = Render<Kitchen>();

        cut.Find("[data-kitchen='button'] .ui-button--icon svg").GetAttribute("fill").Should().Be("none");
        cut.Find("[data-kitchen='button'] .ui-button--icon").GetAttribute("aria-label").Should().Be("Adicionar");
    }

    [Fact]
    public void KitchenPage_IconsSection_ShowsCatalogAndSizes()
    {
        var cut = Render<Kitchen>();

        cut.FindAll("[data-kitchen='icons'] svg").Count.Should().BeGreaterThanOrEqualTo(27);
        cut.Find("[data-kitchen='icons'] .ui-icon--default").Should().NotBeNull();
        cut.Find("[data-kitchen='icons'] .ui-icon--topbar").Should().NotBeNull();
        cut.Find("[data-kitchen='icons'] .ui-icon--auth-hero").Should().NotBeNull();
        cut.Find("[data-kitchen='icons'] .ui-icon--empty-state").Should().NotBeNull();
    }
}
