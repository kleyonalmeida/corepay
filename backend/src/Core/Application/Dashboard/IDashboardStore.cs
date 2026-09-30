using BuildingBlocks.Results;
using Core.Application.Payrolls;

namespace Core.Application.Dashboard;

public interface IDashboardStore
{
    Task<Result<DashboardResponse>> GetAsync(
        DashboardFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);
}
