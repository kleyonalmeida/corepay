using FluentAssertions;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Layout;

public class SidebarStateTests
{
    [Fact]
    public void ToggleDesktopCollapse_FlipsState()
    {
        var state = new SidebarState();

        state.IsDesktopCollapsed.Should().BeFalse();

        state.ToggleDesktopCollapse();
        state.IsDesktopCollapsed.Should().BeTrue();

        state.ToggleDesktopCollapse();
        state.IsDesktopCollapsed.Should().BeFalse();
    }

    [Fact]
    public void ToggleMobileDrawer_FlipsState()
    {
        var state = new SidebarState();

        state.IsMobileOpen.Should().BeFalse();

        state.ToggleMobileDrawer();
        state.IsMobileOpen.Should().BeTrue();

        state.ToggleMobileDrawer();
        state.IsMobileOpen.Should().BeFalse();
    }

    [Fact]
    public void CloseMobileDrawer_WhenOpen_SetsFalse()
    {
        var state = new SidebarState();
        state.ToggleMobileDrawer();

        state.CloseMobileDrawer();

        state.IsMobileOpen.Should().BeFalse();
    }

    [Fact]
    public void CloseMobileDrawer_WhenClosed_IsNoOp()
    {
        var state = new SidebarState();
        var notifications = 0;
        state.StateChanged += () => notifications++;

        state.CloseMobileDrawer();

        notifications.Should().Be(0);
        state.IsMobileOpen.Should().BeFalse();
    }

    [Fact]
    public void StateChanged_RaisedOnToggle()
    {
        var state = new SidebarState();
        var notifications = 0;
        state.StateChanged += () => notifications++;

        state.ToggleDesktopCollapse();
        state.ToggleMobileDrawer();

        notifications.Should().Be(2);
    }
}
