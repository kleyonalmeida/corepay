namespace WebApp.Blazor.Services;

public interface IAnalystMetricApiService
{
    Task<AnalystMetricListResult> GetMetricsAsync(
        AnalystMetricListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<AnalystMetricMutationResult> CreateMetricAsync(
        AnalystMetricRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalystMetricMutationResult> UpdateMetricAsync(
        Guid id,
        AnalystMetricRequest request,
        CancellationToken cancellationToken = default);
}
