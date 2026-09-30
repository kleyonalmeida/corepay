namespace WebApp.Blazor.Services;

public interface IProjectApiService
{
    Task<ProjectListResult> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<ProjectMutationResult> CreateProjectAsync(
        ProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectMutationResult> UpdateProjectAsync(
        Guid id,
        ProjectRequest request,
        CancellationToken cancellationToken = default);
}
