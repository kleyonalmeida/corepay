using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddCorePayAuth(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddScoped<IAuthSessionStorage, JsAuthSessionStorage>();
        services.AddScoped<CorePayAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        services.AddScoped<AuthService>();
        services.AddScoped<AuthorizationMessageHandler>();

        return services;
    }
}
