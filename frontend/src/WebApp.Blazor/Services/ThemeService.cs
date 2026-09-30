using Microsoft.JSInterop;

namespace WebApp.Blazor.Services;

public sealed class ThemeService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "theme";
    private const string DefaultTheme = "dark";

    private string _currentTheme = DefaultTheme;

    public event Action? OnThemeChanged;

    public string CurrentTheme => _currentTheme;

    public bool IsDark => _currentTheme == "dark";

    public async Task InitializeAsync()
    {
        var stored = await jsRuntime.InvokeAsync<string>("corepayTheme.get");
        _currentTheme = NormalizeTheme(stored);
    }

    public async Task ToggleAsync()
    {
        _currentTheme = IsDark ? "light" : "dark";
        await ApplyAsync();
    }

    public async Task ApplyAsync()
    {
        await jsRuntime.InvokeVoidAsync("corepayTheme.apply", _currentTheme);
        OnThemeChanged?.Invoke();
    }

    private static string NormalizeTheme(string? stored)
    {
        return stored switch
        {
            "light" => "light",
            "dark" => "dark",
            _ => DefaultTheme
        };
    }
}
