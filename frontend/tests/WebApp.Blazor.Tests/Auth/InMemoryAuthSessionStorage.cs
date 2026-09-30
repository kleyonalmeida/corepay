using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

internal sealed class InMemoryAuthSessionStorage : IAuthSessionStorage
{
    private AuthSession? _session;

    public Task<AuthSession?> GetAsync()
    {
        if (_session is null || _session.IsExpired(DateTime.UtcNow))
        {
            _session = null;
            return Task.FromResult<AuthSession?>(null);
        }

        return Task.FromResult<AuthSession?>(_session);
    }

    public Task SetAsync(AuthSession session)
    {
        _session = session;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _session = null;
        return Task.CompletedTask;
    }
}
