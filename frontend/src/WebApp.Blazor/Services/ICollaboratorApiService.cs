namespace WebApp.Blazor.Services;

public interface ICollaboratorApiService
{
    Task<CollaboratorListResult> GetCollaboratorsAsync(
        CollaboratorListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<CollaboratorGetResult> GetCollaboratorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CollaboratorMutationResult> CreateCollaboratorAsync(
        CollaboratorRequest request,
        CancellationToken cancellationToken = default);

    Task<CollaboratorMutationResult> UpdateCollaboratorAsync(
        Guid id,
        CollaboratorRequest request,
        CancellationToken cancellationToken = default);
}
