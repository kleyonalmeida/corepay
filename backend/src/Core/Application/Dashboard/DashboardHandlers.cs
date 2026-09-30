using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Dashboard;

public sealed class GetDashboardHandler(IDashboardStore store)
    : IRequestHandler<GetDashboardQuery, Result<DashboardResponse>>
{
    public Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken) =>
        store.GetAsync(request.Filters, request.Access, cancellationToken);
}
