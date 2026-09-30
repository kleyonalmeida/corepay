namespace WebApp.Blazor.Services;

public interface IFinanceApiService
{
    Task<FinanceSummaryResult> GetSummaryAsync(
        FinanceSummaryQuery? query = null,
        CancellationToken cancellationToken = default);
}
