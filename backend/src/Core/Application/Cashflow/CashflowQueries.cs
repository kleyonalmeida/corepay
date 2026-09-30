using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Cashflow;

public sealed record GetCashflowEntriesQuery(CashflowListFilters Filters)
    : IRequest<Result<CashflowListResponse>>;

public sealed record GetCashflowEntryByIdQuery(Guid EntryId)
    : IRequest<Result<CashflowEntryResponse>>;

public sealed record GetCashflowInstallmentGroupQuery(Guid CompraId)
    : IRequest<Result<IReadOnlyList<CashflowEntryResponse>>>;

public sealed record GetCashflowReportQuery(CashflowReportFilters Filters)
    : IRequest<Result<CashflowReportResponse>>;
