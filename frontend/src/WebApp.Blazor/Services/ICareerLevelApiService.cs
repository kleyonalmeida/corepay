namespace WebApp.Blazor.Services;

public interface ICareerLevelApiService
{
    Task<CareerLevelListResult> GetCareerLevelsAsync(CancellationToken cancellationToken = default);

    Task<CareerLevelMutationResult> CreateCareerLevelAsync(
        CareerLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<CareerLevelMutationResult> UpdateCareerLevelAsync(
        Guid id,
        CareerLevelRequest request,
        CancellationToken cancellationToken = default);
}
