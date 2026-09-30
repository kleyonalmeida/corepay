using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Traffic;

public sealed record CreateTrafficInvestmentCommand(CreateTrafficInvestmentRequest Request)
    : IRequest<Result<TrafficInvestmentResponse>>;

public sealed record UpdateTrafficInvestmentCommand(Guid TrafficInvestmentId, UpdateTrafficInvestmentRequest Request)
    : IRequest<Result<TrafficInvestmentResponse>>;

public sealed record CreateTrafficProjectDepositCommand(CreateTrafficProjectDepositRequest Request)
    : IRequest<Result<TrafficProjectDepositResponse>>;
