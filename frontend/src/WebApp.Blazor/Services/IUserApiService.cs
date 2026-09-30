namespace WebApp.Blazor.Services;

public interface IUserApiService
{
    Task<UserListResult> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserMutationResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserMutationResult> UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDeleteResult> DeleteUserAsync(
        string id,
        CancellationToken cancellationToken = default);
}
