using BuildingBlocks.Results;

namespace Core.Application.Collaborators;

public interface ICollaboratorStore
{
    Task<Result<CollaboratorsListResponse>> GetCollaboratorsAsync(
        Guid? departmentId,
        string? search,
        bool? isActive,
        CollaboratorAccessContext access,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<Result<CollaboratorResponse>> GetCollaboratorByIdAsync(
        Guid collaboratorId,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<CollaboratorResponse>> CreateCollaboratorAsync(
        CreateCollaboratorRequest request,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<CollaboratorResponse>> UpdateCollaboratorAsync(
        Guid collaboratorId,
        UpdateCollaboratorRequest request,
        CollaboratorAccessContext access,
        CancellationToken cancellationToken = default);
}
