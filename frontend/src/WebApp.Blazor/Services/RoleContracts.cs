namespace WebApp.Blazor.Services;

public enum RoleApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public sealed record RoleDto(
    string Id,
    string Name,
    IReadOnlyList<string> PermissionKeys);

public sealed record RoleListResult(
    RoleApiStatus Status,
    IReadOnlyList<RoleDto>? Roles = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PermissionDto(Guid Id, string Key, string Description);

public sealed record PermissionListResult(
    RoleApiStatus Status,
    IReadOnlyList<PermissionDto>? Permissions = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CreateRoleRequest(string Name, IReadOnlyList<string> PermissionKeys);

public sealed record UpdateRoleRequest(string Name, IReadOnlyList<string> PermissionKeys);

public sealed record RoleMutationResult(
    RoleApiStatus Status,
    RoleDto? Role = null,
    string? ErrorCode = null,
    string? Message = null);
