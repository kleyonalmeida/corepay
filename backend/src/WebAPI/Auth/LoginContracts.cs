namespace WebAPI.Auth;

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
