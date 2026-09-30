using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

internal static class AuthTestHelper
{
    public static async Task AuthenticateAsync(
        IServiceProvider services,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        string displayName = "Usuário Teste")
    {
        var storage = services.GetRequiredService<IAuthSessionStorage>();
        var stateProvider = services.GetRequiredService<CorePayAuthenticationStateProvider>();
        var session = new AuthSession(
            "token",
            DateTime.UtcNow.AddHours(1),
            new LoginUserResponse(
                "user-id",
                "user@test.com",
                displayName,
                roles,
                permissions));

        await storage.SetAsync(session);
        stateProvider.SetSession(session);
    }
}
