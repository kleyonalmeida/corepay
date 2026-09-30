using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Services;

public sealed class AuthorizationMessageHandler(
    CorePayAuthenticationStateProvider authenticationStateProvider,
    IAuthSessionStorage sessionStorage) : DelegatingHandler
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var session = authenticationStateProvider.CurrentSession;
        if (session is not null
            && !session.IsExpired(DateTime.UtcNow)
            && !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ClearSessionAsync();
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden
                 && request.RequestUri?.AbsolutePath.EndsWith("/api/v1/auth/me", StringComparison.OrdinalIgnoreCase)
                     != true)
        {
            await RefreshCurrentUserAsync(session, request.RequestUri, cancellationToken);
        }

        return response;
    }

    private async Task RefreshCurrentUserAsync(
        AuthSession? session,
        Uri? originalRequestUri,
        CancellationToken cancellationToken)
    {
        if (session is null || session.IsExpired(DateTime.UtcNow))
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var refreshUri = originalRequestUri?.IsAbsoluteUri == true
                ? new Uri(originalRequestUri, "/api/v1/auth/me")
                : new Uri("/api/v1/auth/me", UriKind.Relative);
            using var refreshRequest = new HttpRequestMessage(HttpMethod.Get, refreshUri);
            refreshRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);

            using var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
            if (refreshResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ClearSessionAsync();
                return;
            }

            if (!refreshResponse.IsSuccessStatusCode)
            {
                return;
            }

            CurrentUserResponse? currentUser;
            try
            {
                currentUser = await refreshResponse.Content.ReadFromJsonAsync<CurrentUserResponse>(
                    cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                return;
            }

            if (currentUser is null)
            {
                return;
            }

            var refreshed = authenticationStateProvider.ReplaceCurrentUser(new LoginUserResponse(
                currentUser.Id,
                currentUser.Email,
                currentUser.DisplayName,
                currentUser.Roles,
                currentUser.Permissions));
            if (refreshed is not null)
            {
                await sessionStorage.SetAsync(refreshed);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task ClearSessionAsync()
    {
        await sessionStorage.ClearAsync();
        authenticationStateProvider.SetSession(null);
    }
}
