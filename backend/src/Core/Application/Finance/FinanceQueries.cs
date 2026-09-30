using BuildingBlocks.Results;
using Core.Application.Payrolls;
using MediatR;

namespace Core.Application.Finance;

public sealed record GetFinanceSummaryQuery(
    FinanceSummaryFilters Filters,
    PayrollAccessContext Access) : IRequest<Result<FinanceSummaryResponse>>;
