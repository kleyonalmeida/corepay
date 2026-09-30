using Bunit;
using InfiniLore.Lucide;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using WebApp.Blazor.Services;
using SessionIdleOptions = WebApp.Blazor.Services.SessionIdleOptions;

namespace WebApp.Blazor.Tests;

/// <summary>
/// Base bUnit context with serviços compartilhados do frontend (tema + ícones Lucide).
/// </summary>
public abstract class BlazorComponentTestContext : BunitContext
{
    protected BlazorComponentTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.Configure<SessionIdleOptions>(_ => { });
        Services.AddScoped<ThemeService>();
        Services.AddLucideIcons();
    }
}
