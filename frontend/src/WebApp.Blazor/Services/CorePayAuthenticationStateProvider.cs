using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Services;

public sealed class CorePayAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthSession? _session;

    public AuthSession? CurrentSession => _session;

    public bool IsAuthenticated => _session is not null && !_session.IsExpired(DateTime.UtcNow);

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_session is null || _session.IsExpired(DateTime.UtcNow))
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        return Task.FromResult(new AuthenticationState(CreatePrincipal(_session)));
    }

    public void SetSession(AuthSession? session)
    {
        _session = session;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public AuthSession? ReplaceCurrentUser(LoginUserResponse user)
    {
        if (_session is null || _session.IsExpired(DateTime.UtcNow))
        {
            return null;
        }

        _session = _session with { User = user };
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return _session;
    }

    internal static ClaimsPrincipal CreatePrincipal(AuthSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.Id),
            new(ClaimTypes.Email, session.User.Email),
            new(ClaimTypes.Name, session.User.DisplayName)
        };

        foreach (var role in session.User.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in session.User.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }
}
