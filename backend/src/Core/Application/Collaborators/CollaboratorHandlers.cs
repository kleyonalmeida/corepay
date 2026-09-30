using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Collaborators;

public sealed class GetCollaboratorsHandler(ICollaboratorStore store)
    : IRequestHandler<GetCollaboratorsQuery, Result<CollaboratorsListResponse>>
{
    public Task<Result<CollaboratorsListResponse>> Handle(
        GetCollaboratorsQuery request,
        CancellationToken cancellationToken) =>
        store.GetCollaboratorsAsync(
            request.DepartmentId,
            request.Search,
            request.IsActive,
            request.Access,
            request.Page,
            request.PageSize,
            cancellationToken);
}

public sealed class GetCollaboratorByIdHandler(ICollaboratorStore store)
    : IRequestHandler<GetCollaboratorByIdQuery, Result<CollaboratorResponse>>
{
    public Task<Result<CollaboratorResponse>> Handle(
        GetCollaboratorByIdQuery request,
        CancellationToken cancellationToken) =>
        store.GetCollaboratorByIdAsync(request.CollaboratorId, request.Access, cancellationToken);
}

public sealed class CreateCollaboratorHandler(ICollaboratorStore store)
    : IRequestHandler<CreateCollaboratorCommand, Result<CollaboratorResponse>>
{
    public Task<Result<CollaboratorResponse>> Handle(
        CreateCollaboratorCommand request,
        CancellationToken cancellationToken) =>
        store.CreateCollaboratorAsync(request.Request, request.Access, cancellationToken);
}

public sealed class UpdateCollaboratorHandler(ICollaboratorStore store)
    : IRequestHandler<UpdateCollaboratorCommand, Result<CollaboratorResponse>>
{
    public Task<Result<CollaboratorResponse>> Handle(
        UpdateCollaboratorCommand request,
        CancellationToken cancellationToken) =>
        store.UpdateCollaboratorAsync(request.CollaboratorId, request.Request, request.Access, cancellationToken);
}
