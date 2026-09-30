namespace WebApp.Blazor.Services;

public interface ICashflowApiService
{
    Task<CashflowListResult> GetEntriesAsync(
        CashflowListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<CashflowDetailResult> GetEntryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CashflowCreateResult> CreateEntryAsync(
        CashflowCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<CashflowMutationResult> UpdateEntryAsync(
        Guid id,
        CashflowUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<CashflowDeleteResult> DeleteEntryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CashflowInstallmentGroupResult> GetInstallmentGroupAsync(
        Guid compraId,
        CancellationToken cancellationToken = default);

    Task<CashflowReportResult> GetReportAsync(
        CashflowReportQuery query,
        CancellationToken cancellationToken = default);
}
