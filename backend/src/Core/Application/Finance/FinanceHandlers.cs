using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Finance;

public sealed class GetFinanceSummaryHandler(IFinanceStore store)
    : IRequestHandler<GetFinanceSummaryQuery, Result<FinanceSummaryResponse>>
{
    public Task<Result<FinanceSummaryResponse>> Handle(
        GetFinanceSummaryQuery request,
        CancellationToken cancellationToken) =>
        store.GetSummaryAsync(request.Filters, request.Access, cancellationToken);
}
