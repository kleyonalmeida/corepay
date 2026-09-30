using BuildingBlocks.Results;
using Core.Domain;

namespace Core.Application.Payrolls;

public interface IPayrollStore
{
    Task<Result<Payroll>> SaveAsync(Payroll payroll, CancellationToken cancellationToken = default);

    Task<Result<Payroll>> GetByIdAsync(Guid payrollId, CancellationToken cancellationToken = default);

    Task<Result<PayrollsListResponse>> GetPayrollsAsync(
        PayrollListFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollSummaryResponse>> GetSummaryByIdAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollSummaryResponse>> DuplicateToNextMonthAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollFormOptionsResponse>> GetFormOptionsAsync(
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> CreatePayrollAsync(
        CreatePayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> GetDetailByIdAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> UpdatePayrollShellAsync(
        Guid payrollId,
        UpdatePayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollEntryPreviewResponse>> PreviewEntryAsync(
        Guid payrollId,
        Guid entryId,
        PreviewPayrollEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> SubmitPayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> ApprovePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> RejectPayrollAsync(
        Guid payrollId,
        RejectPayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> RecalculatePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result> DeletePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> PayPayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> ApproveEntryAsync(
        Guid payrollId,
        Guid entryId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> PayEntryAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryPaidRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> SetEntryNfAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryNfRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> UpdateEntryAsync(
        Guid payrollId,
        Guid entryId,
        UpdatePayrollEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);

    Task<Result<PayrollDetailResponse>> AddCollaboratorEntryAsync(
        Guid payrollId,
        AddCollaboratorEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default);
}
