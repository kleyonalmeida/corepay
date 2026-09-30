using BuildingBlocks.Results;
using Core.Application.Payrolls;
using MediatR;

namespace Core.Application.Dashboard;

public sealed record GetDashboardQuery(
    DashboardFilters Filters,
    PayrollAccessContext Access) : IRequest<Result<DashboardResponse>>;
