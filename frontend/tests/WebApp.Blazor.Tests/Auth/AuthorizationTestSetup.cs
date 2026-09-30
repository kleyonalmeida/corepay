using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

internal static class AuthorizationTestSetup
{
    public static void AddCorePayAuthorization(IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
    }
}
