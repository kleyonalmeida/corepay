namespace WebApp.Blazor.Services;

public interface IProjectRevenueApiService
{
    Task<ProjectRevenueListResult> GetRevenuesAsync(
        ProjectRevenueListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<ProjectRevenueDetailResult> GetRevenueByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProjectRevenueMutationResult> CreateRevenueAsync(
        ProjectRevenueRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectRevenueMutationResult> UpdateRevenueAsync(
        Guid id,
        ProjectRevenueRequest request,
        CancellationToken cancellationToken = default);
}
