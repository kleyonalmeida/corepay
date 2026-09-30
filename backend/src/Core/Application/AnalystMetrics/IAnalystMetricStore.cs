using BuildingBlocks.Results;

namespace Core.Application.AnalystMetrics;

public interface IAnalystMetricStore
{
    Task<Result<IReadOnlyList<AnalystMetricResponse>>> GetListAsync(
        AnalystMetricListFilters filters,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<AnalystMetricResponse>> GetByIdAsync(
        Guid analystMetricId,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<AnalystMetricResponse>> CreateAsync(
        CreateAnalystMetricRequest request,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<AnalystMetricResponse>> UpdateAsync(
        Guid analystMetricId,
        UpdateAnalystMetricRequest request,
        AnalystMetricAccessContext access,
        CancellationToken cancellationToken = default);
}
