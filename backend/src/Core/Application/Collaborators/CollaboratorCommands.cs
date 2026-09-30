using BuildingBlocks.Results;
using MediatR;

namespace Core.Application.Collaborators;

public sealed record CreateCollaboratorCommand(
    CreateCollaboratorRequest Request,
    CollaboratorAccessContext Access) : IRequest<Result<CollaboratorResponse>>;

public sealed record UpdateCollaboratorCommand(
    Guid CollaboratorId,
    UpdateCollaboratorRequest Request,
    CollaboratorAccessContext Access) : IRequest<Result<CollaboratorResponse>>;
