namespace WebApp.Blazor.Services;

public enum UserApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<Guid> DepartmentIds);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string DisplayName,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<Guid> DepartmentIds);

public sealed record UpdateUserRequest(
    string Email,
    string DisplayName,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<Guid> DepartmentIds);

public sealed record UserListResult(
    UserApiStatus Status,
    IReadOnlyList<UserDto>? Users = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record UserMutationResult(
    UserApiStatus Status,
    UserDto? User = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record UserDeleteResult(
    UserApiStatus Status,
    string? ErrorCode = null,
    string? Message = null);
