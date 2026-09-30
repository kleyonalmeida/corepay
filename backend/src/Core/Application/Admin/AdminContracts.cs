namespace Core.Application.Admin;

public sealed record RoleResponse(string Id, string Name, IReadOnlyList<string> PermissionKeys);

public sealed record CreateRoleRequest(string Name, IReadOnlyList<string> PermissionKeys);

public sealed record UpdateRoleRequest(string Name, IReadOnlyList<string> PermissionKeys);

public sealed record PermissionResponse(Guid Id, string Key, string Description);

public sealed record CreatePermissionRequest(string Key, string Description);

public sealed record UpdatePermissionRequest(string Description);

public sealed record UserResponse(
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
