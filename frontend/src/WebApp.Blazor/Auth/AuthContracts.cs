namespace WebApp.Blazor.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    LoginUserResponse User);

public sealed record LoginUserResponse(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record CurrentUserResponse(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> DepartmentIds);

public sealed record AuthSession(
    string AccessToken,
    DateTime ExpiresAtUtc,
    LoginUserResponse User)
{
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}

public sealed record AuthResult(bool Succeeded, string? ErrorMessage = null);
