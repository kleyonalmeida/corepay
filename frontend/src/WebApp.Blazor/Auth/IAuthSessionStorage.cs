namespace WebApp.Blazor.Auth;

public interface IAuthSessionStorage
{
    Task<AuthSession?> GetAsync();

    Task SetAsync(AuthSession session);

    Task ClearAsync();
}
