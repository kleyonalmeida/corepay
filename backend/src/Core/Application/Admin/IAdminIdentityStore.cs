using BuildingBlocks.Results;

namespace Core.Application.Admin;

public interface IAdminIdentityStore
{
    Task<Result<IReadOnlyList<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<Result<RoleResponse>> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result<RoleResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PermissionResponse>>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<Result<PermissionResponse>> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);

    Task<Result<PermissionResponse>> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);

    Task<Result<PermissionResponse>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UserResponse>>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        string actingUserId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteUserAsync(string userId, string actingUserId, CancellationToken cancellationToken = default);
}
