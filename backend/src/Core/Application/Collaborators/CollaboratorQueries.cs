using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Collaborators;

public sealed record GetCollaboratorsQuery(
    Guid? DepartmentId,
    string? Search,
    bool? IsActive,
    CollaboratorAccessContext Access,
    int? Page = null,
    int? PageSize = null) : IRequest<Result<CollaboratorsListResponse>>;

public sealed record GetCollaboratorByIdQuery(
    Guid CollaboratorId,
    CollaboratorAccessContext Access) : IRequest<Result<CollaboratorResponse>>;
