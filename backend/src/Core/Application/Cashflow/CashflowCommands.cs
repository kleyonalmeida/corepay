using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Cashflow;

public sealed record CreateCashflowEntryCommand(CreateCashflowEntryRequest Request)
    : IRequest<Result<CashflowCreateResponse>>;

public sealed record UpdateCashflowEntryCommand(Guid EntryId, UpdateCashflowEntryRequest Request)
    : IRequest<Result<CashflowEntryResponse>>;

public sealed record DeleteCashflowEntryCommand(Guid EntryId)
    : IRequest<Result>;
