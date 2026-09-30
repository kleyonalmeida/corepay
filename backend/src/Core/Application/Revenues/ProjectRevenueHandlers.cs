using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Revenues;

public sealed class GetProjectRevenuesHandler(IProjectRevenueStore store)
    : IRequestHandler<GetProjectRevenuesQuery, Result<IReadOnlyList<ProjectRevenueResponse>>>
{
    public Task<Result<IReadOnlyList<ProjectRevenueResponse>>> Handle(
        GetProjectRevenuesQuery request,
        CancellationToken cancellationToken) =>
        store.GetListAsync(request.Filters, cancellationToken);
}

public sealed class GetProjectRevenueByIdHandler(IProjectRevenueStore store)
    : IRequestHandler<GetProjectRevenueByIdQuery, Result<ProjectRevenueResponse>>
{
    public Task<Result<ProjectRevenueResponse>> Handle(
        GetProjectRevenueByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetByIdAsync(request.ProjectRevenueId, cancellationToken);
}

public sealed class CreateProjectRevenueHandler(IProjectRevenueStore store)
    : IRequestHandler<CreateProjectRevenueCommand, Result<ProjectRevenueResponse>>
{
    public Task<Result<ProjectRevenueResponse>> Handle(
        CreateProjectRevenueCommand request,
        CancellationToken cancellationToken) =>
        store.CreateAsync(request.Request, cancellationToken);
}

public sealed class UpdateProjectRevenueHandler(IProjectRevenueStore store)
    : IRequestHandler<UpdateProjectRevenueCommand, Result<ProjectRevenueResponse>>
{
    public Task<Result<ProjectRevenueResponse>> Handle(
        UpdateProjectRevenueCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateAsync(request.ProjectRevenueId, request.Request, cancellationToken);
}
