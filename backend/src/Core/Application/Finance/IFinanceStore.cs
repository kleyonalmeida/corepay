using BuildingBlocks.Results;
using Core.Application.Payrolls;

namespace Core.Application.Finance;

public interface IFinanceStore
{
    Task<Result<FinanceSummaryResponse>> GetSummaryAsync(
        FinanceSummaryFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);
}
