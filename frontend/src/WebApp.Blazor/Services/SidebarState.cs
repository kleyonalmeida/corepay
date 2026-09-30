namespace WebApp.Blazor.Services;

/// <summary>
/// Estado efêmero do shell (collapse desktop e drawer mobile). Não persiste em localStorage.
/// </summary>
public sealed class SidebarState
{
    public bool IsDesktopCollapsed { get; private set; }

    public bool IsMobileOpen { get; private set; }

    public event Action? StateChanged;

    public void ToggleDesktopCollapse()
    {
        IsDesktopCollapsed = !IsDesktopCollapsed;
        Notify();
    }

    public void ToggleMobileDrawer()
    {
        IsMobileOpen = !IsMobileOpen;
        Notify();
    }

    public void CloseMobileDrawer()
    {
        if (!IsMobileOpen)
        {
            return;
        }

        IsMobileOpen = false;
        Notify();
    }

    private void Notify() => StateChanged?.Invoke();
}
