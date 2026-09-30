using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Traffic;

public sealed record GetTrafficInvestmentsQuery(TrafficInvestmentListFilters Filters)
    : IRequest<Result<IReadOnlyList<TrafficInvestmentListItemResponse>>>;

public sealed record GetTrafficInvestmentByIdQuery(Guid TrafficInvestmentId)
    : IRequest<Result<TrafficInvestmentResponse>>;

public sealed record GetTrafficProjectDepositsQuery(TrafficProjectDepositListFilters Filters)
    : IRequest<Result<IReadOnlyList<TrafficProjectDepositResponse>>>;
