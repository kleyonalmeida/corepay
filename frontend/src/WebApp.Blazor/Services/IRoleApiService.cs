namespace WebApp.Blazor.Services;

public interface IRoleApiService
{
    Task<RoleListResult> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<PermissionListResult> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<RoleMutationResult> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<RoleMutationResult> UpdateRoleAsync(
        string id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);
}
