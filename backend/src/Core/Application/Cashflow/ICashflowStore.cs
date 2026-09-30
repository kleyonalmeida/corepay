using BuildingBlocks.Results;

namespace Core.Application.Cashflow;

public interface ICashflowStore
{
    Task<Result<CashflowListResponse>> GetListAsync(
        CashflowListFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<CashflowEntryResponse>> GetByIdAsync(
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task<Result<CashflowCreateResponse>> CreateAsync(
        CreateCashflowEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CashflowEntryResponse>> UpdateAsync(
        Guid entryId,
        UpdateCashflowEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CashflowEntryResponse>>> GetInstallmentGroupAsync(
        Guid compraId,
        CancellationToken cancellationToken = default);

    Task<Result<CashflowReportResponse>> GetReportAsync(
        CashflowReportFilters filters,
        CancellationToken cancellationToken = default);

    Task<Result<FacilitiesCashflowWebhookResponse>> CreateFromFacilitiesWebhookAsync(
        FacilitiesCashflowWebhookRequest request,
        CancellationToken cancellationToken = default);
}
