using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Services;

public sealed class AuthService
{
    private const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly HttpClient _httpClient;
    private readonly IAuthSessionStorage _sessionStorage;
    private readonly CorePayAuthenticationStateProvider _authenticationStateProvider;
    private bool _initialized;

    public AuthService(
        HttpClient httpClient,
        IAuthSessionStorage sessionStorage,
        CorePayAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _sessionStorage = sessionStorage;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public AuthSession? CurrentSession => _authenticationStateProvider.CurrentSession;

    public bool IsAuthenticated => _authenticationStateProvider.IsAuthenticated;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        var session = await _sessionStorage.GetAsync();
        if (session is null || session.IsExpired(DateTime.UtcNow))
        {
            await ClearSessionAsync();
            _initialized = true;
            return;
        }

        _authenticationStateProvider.SetSession(session);
        await RefreshCurrentUserAsync();
        _initialized = true;
    }

    public async Task<bool> RefreshCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var session = _authenticationStateProvider.CurrentSession;
        if (session is null || session.IsExpired(DateTime.UtcNow))
        {
            await ClearSessionAsync();
            return false;
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("/api/v1/auth/me", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return false;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ClearSessionAsync();
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        CurrentUserResponse? currentUser;
        try
        {
            currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return false;
        }

        if (currentUser is null)
        {
            return false;
        }

        var refreshed = _authenticationStateProvider.ReplaceCurrentUser(new LoginUserResponse(
            currentUser.Id,
            currentUser.Email,
            currentUser.DisplayName,
            currentUser.Roles,
            currentUser.Permissions));
        if (refreshed is null)
        {
            return false;
        }

        await _sessionStorage.SetAsync(refreshed);
        return true;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResult(false, InvalidCredentialsMessage);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(email.Trim(), password));
        }
        catch (HttpRequestException)
        {
            return new AuthResult(false, "Não foi possível conectar ao servidor. Tente novamente.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new AuthResult(false, InvalidCredentialsMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new AuthResult(false, "Não foi possível autenticar. Tente novamente.");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (loginResponse is null
            || string.IsNullOrWhiteSpace(loginResponse.AccessToken)
            || loginResponse.User is null)
        {
            return new AuthResult(false, InvalidCredentialsMessage);
        }

        var session = new AuthSession(
            loginResponse.AccessToken,
            loginResponse.ExpiresAtUtc,
            loginResponse.User);

        await _sessionStorage.SetAsync(session);
        _authenticationStateProvider.SetSession(session);

        return new AuthResult(true);
    }

    public async Task LogoutAsync()
    {
        await ClearSessionAsync();
    }

    private async Task ClearSessionAsync()
    {
        await _sessionStorage.ClearAsync();
        _authenticationStateProvider.SetSession(null);
    }
}
