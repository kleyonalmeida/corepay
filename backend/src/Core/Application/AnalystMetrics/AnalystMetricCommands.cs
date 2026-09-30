using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.AnalystMetrics;

public sealed record CreateAnalystMetricCommand(
    CreateAnalystMetricRequest Request,
    AnalystMetricAccessContext Access)
    : IRequest<Result<AnalystMetricResponse>>;

public sealed record UpdateAnalystMetricCommand(
    Guid AnalystMetricId,
    UpdateAnalystMetricRequest Request,
    AnalystMetricAccessContext Access)
    : IRequest<Result<AnalystMetricResponse>>;
