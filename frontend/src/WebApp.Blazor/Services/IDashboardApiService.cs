namespace WebApp.Blazor.Services;

public interface IDashboardApiService
{
    Task<DashboardResult> GetAsync(
        DashboardQuery? query = null,
        CancellationToken cancellationToken = default);
}
