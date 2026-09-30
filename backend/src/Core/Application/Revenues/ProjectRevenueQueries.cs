using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Revenues;

public sealed record GetProjectRevenuesQuery(ProjectRevenueListFilters Filters)
    : IRequest<Result<IReadOnlyList<ProjectRevenueResponse>>>;

public sealed record GetProjectRevenueByIdQuery(Guid ProjectRevenueId)
    : IRequest<Result<ProjectRevenueResponse>>;
