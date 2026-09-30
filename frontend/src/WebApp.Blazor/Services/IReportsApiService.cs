namespace WebApp.Blazor.Services;

public interface IReportsApiService
{
    Task<ReportsResult> GetPayrollReportAsync(
        ReportsQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<ReportsExportResult> ExportPayrollReportAsync(
        ReportsQuery? query = null,
        CancellationToken cancellationToken = default);
}
