using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.AnalystMetrics;

public sealed record GetAnalystMetricsQuery(
    AnalystMetricListFilters Filters,
    AnalystMetricAccessContext Access)
    : IRequest<Result<IReadOnlyList<AnalystMetricResponse>>>;

public sealed record GetAnalystMetricByIdQuery(
    Guid AnalystMetricId,
    AnalystMetricAccessContext Access)
    : IRequest<Result<AnalystMetricResponse>>;
