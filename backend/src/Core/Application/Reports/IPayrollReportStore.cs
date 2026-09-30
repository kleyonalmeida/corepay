using BuildingBlocks.Results;
using Core.Application.Payrolls;

namespace Core.Application.Reports;

public interface IPayrollReportStore
{
    Task<Result<PayrollReportResponse>> GetPayrollReportAsync(
        PayrollReportFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);
}
