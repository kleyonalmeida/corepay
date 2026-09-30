using BuildingBlocks.Results;

namespace Core.Application.Revenues;

public interface IProjectRevenueStore
{
    Task<Result<IReadOnlyList<ProjectRevenueResponse>>> GetListAsync(
        ProjectRevenueListFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<ProjectRevenueResponse>> GetByIdAsync(
        Guid projectRevenueId,
        CancellationToken cancellationToken = default);

    Task<Result<ProjectRevenueResponse>> CreateAsync(
        CreateProjectRevenueRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProjectRevenueResponse>> UpdateAsync(
        Guid projectRevenueId,
        UpdateProjectRevenueRequest request,
        CancellationToken cancellationToken = default);
}
