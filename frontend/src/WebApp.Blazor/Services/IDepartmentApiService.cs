namespace WebApp.Blazor.Services;

public interface IDepartmentApiService
{
    Task<DepartmentListResult> GetDepartmentsAsync(CancellationToken cancellationToken = default);

    Task<DepartmentMutationResult> CreateDepartmentAsync(
        DepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentMutationResult> UpdateDepartmentAsync(
        Guid id,
        DepartmentRequest request,
        CancellationToken cancellationToken = default);
}
