using System.Text.Json;
using Microsoft.JSInterop;

namespace WebApp.Blazor.Auth;

public sealed class JsAuthSessionStorage(IJSRuntime jsRuntime) : IAuthSessionStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<AuthSession?> GetAsync()
    {
        var raw = await jsRuntime.InvokeAsync<string?>("corepayAuth.getSession");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var session = JsonSerializer.Deserialize<AuthSession>(raw, JsonOptions);
            if (session is null
                || string.IsNullOrWhiteSpace(session.AccessToken)
                || session.User is null
                || string.IsNullOrWhiteSpace(session.User.Id))
            {
                await ClearAsync();
                return null;
            }

            if (session.IsExpired(DateTime.UtcNow))
            {
                await ClearAsync();
                return null;
            }

            return session;
        }
        catch (JsonException)
        {
            await ClearAsync();
            return null;
        }
    }

    public async Task SetAsync(AuthSession session)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await jsRuntime.InvokeVoidAsync("corepayAuth.setSession", json);
    }

    public Task ClearAsync() =>
        jsRuntime.InvokeVoidAsync("corepayAuth.clearSession").AsTask();
}
