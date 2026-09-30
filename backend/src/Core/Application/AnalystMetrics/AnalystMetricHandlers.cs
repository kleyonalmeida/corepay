using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.AnalystMetrics;

public sealed class GetAnalystMetricsHandler(IAnalystMetricStore store)
    : IRequestHandler<GetAnalystMetricsQuery, Result<IReadOnlyList<AnalystMetricResponse>>>
{
    public Task<Result<IReadOnlyList<AnalystMetricResponse>>> Handle(
        GetAnalystMetricsQuery request,
        CancellationToken cancellationToken) =>
        store.GetListAsync(request.Filters, request.Access, cancellationToken);
}

public sealed class GetAnalystMetricByIdHandler(IAnalystMetricStore store)
    : IRequestHandler<GetAnalystMetricByIdQuery, Result<AnalystMetricResponse>>
{
    public Task<Result<AnalystMetricResponse>> Handle(
        GetAnalystMetricByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetByIdAsync(request.AnalystMetricId, request.Access, cancellationToken);
}

public sealed class CreateAnalystMetricHandler(IAnalystMetricStore store)
    : IRequestHandler<CreateAnalystMetricCommand, Result<AnalystMetricResponse>>
{
    public Task<Result<AnalystMetricResponse>> Handle(
        CreateAnalystMetricCommand request,
        CancellationToken cancellationToken) =>
        store.CreateAsync(request.Request, request.Access, cancellationToken);
}

public sealed class UpdateAnalystMetricHandler(IAnalystMetricStore store)
    : IRequestHandler<UpdateAnalystMetricCommand, Result<AnalystMetricResponse>>
{
    public Task<Result<AnalystMetricResponse>> Handle(
        UpdateAnalystMetricCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateAsync(request.AnalystMetricId, request.Request, request.Access, cancellationToken);
}
