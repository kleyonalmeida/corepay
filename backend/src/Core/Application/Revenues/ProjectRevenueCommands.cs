using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Revenues;

public sealed record CreateProjectRevenueCommand(CreateProjectRevenueRequest Request)
    : IRequest<Result<ProjectRevenueResponse>>;

public sealed record UpdateProjectRevenueCommand(Guid ProjectRevenueId, UpdateProjectRevenueRequest Request)
    : IRequest<Result<ProjectRevenueResponse>>;
