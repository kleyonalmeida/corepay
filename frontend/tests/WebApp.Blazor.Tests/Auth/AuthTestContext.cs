using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Auth;

internal sealed class AuthTestContext : IDisposable
{
    private readonly ServiceCollection _serviceCollection;

    public AuthTestContext(
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null,
        string apiBaseUrl = "http://localhost:5000")
    {
        HttpHandler = new StubHttpMessageHandler(responder ?? (_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)));
        SessionStorage = new InMemoryAuthSessionStorage();

        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddSingleton<IAuthSessionStorage>(SessionStorage);
        _serviceCollection.AddScoped<CorePayAuthenticationStateProvider>();
        _serviceCollection.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<CorePayAuthenticationStateProvider>());
        _serviceCollection.AddScoped<AuthorizationMessageHandler>();
        _serviceCollection.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
            handler.InnerHandler = HttpHandler;
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(apiBaseUrl)
            };
        });
        _serviceCollection.AddScoped<AuthService>();

        Provider = _serviceCollection.BuildServiceProvider();
    }

    public StubHttpMessageHandler HttpHandler { get; }

    public InMemoryAuthSessionStorage SessionStorage { get; }

    public ServiceProvider Provider { get; }

    public AuthService AuthService => Provider.GetRequiredService<AuthService>();

    public CorePayAuthenticationStateProvider StateProvider =>
        Provider.GetRequiredService<CorePayAuthenticationStateProvider>();

    public HttpClient HttpClient => Provider.GetRequiredService<HttpClient>();

    public void Dispose() => Provider.Dispose();
}
