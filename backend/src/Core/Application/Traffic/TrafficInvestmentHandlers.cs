using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Traffic;

public sealed class GetTrafficInvestmentsHandler(ITrafficInvestmentStore store)
    : IRequestHandler<GetTrafficInvestmentsQuery, Result<IReadOnlyList<TrafficInvestmentListItemResponse>>>
{
    public Task<Result<IReadOnlyList<TrafficInvestmentListItemResponse>>> Handle(
        GetTrafficInvestmentsQuery request,
        CancellationToken cancellationToken) =>
        store.GetListAsync(request.Filters, cancellationToken);
}

public sealed class GetTrafficInvestmentByIdHandler(ITrafficInvestmentStore store)
    : IRequestHandler<GetTrafficInvestmentByIdQuery, Result<TrafficInvestmentResponse>>
{
    public Task<Result<TrafficInvestmentResponse>> Handle(
        GetTrafficInvestmentByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetByIdAsync(request.TrafficInvestmentId, cancellationToken);
}

public sealed class CreateTrafficInvestmentHandler(ITrafficInvestmentStore store)
    : IRequestHandler<CreateTrafficInvestmentCommand, Result<TrafficInvestmentResponse>>
{
    public Task<Result<TrafficInvestmentResponse>> Handle(
        CreateTrafficInvestmentCommand request,
        CancellationToken cancellationToken) =>
        store.CreateAsync(request.Request, cancellationToken);
}

public sealed class UpdateTrafficInvestmentHandler(ITrafficInvestmentStore store)
    : IRequestHandler<UpdateTrafficInvestmentCommand, Result<TrafficInvestmentResponse>>
{
    public Task<Result<TrafficInvestmentResponse>> Handle(
        UpdateTrafficInvestmentCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateAsync(request.TrafficInvestmentId, request.Request, cancellationToken);
}

public sealed class GetTrafficProjectDepositsHandler(ITrafficInvestmentStore store)
    : IRequestHandler<GetTrafficProjectDepositsQuery, Result<IReadOnlyList<TrafficProjectDepositResponse>>>
{
    public Task<Result<IReadOnlyList<TrafficProjectDepositResponse>>> Handle(
        GetTrafficProjectDepositsQuery request,
        CancellationToken cancellationToken) =>
        store.GetProjectDepositsAsync(request.Filters, cancellationToken);
}

public sealed class CreateTrafficProjectDepositHandler(ITrafficInvestmentStore store)
    : IRequestHandler<CreateTrafficProjectDepositCommand, Result<TrafficProjectDepositResponse>>
{
    public Task<Result<TrafficProjectDepositResponse>> Handle(
        CreateTrafficProjectDepositCommand request,
        CancellationToken cancellationToken) =>
        store.CreateProjectDepositAsync(request.Request, cancellationToken);
}
