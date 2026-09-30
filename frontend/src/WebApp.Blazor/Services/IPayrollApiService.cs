namespace WebApp.Blazor.Services;

public interface IPayrollApiService
{
    Task<PayrollListResult> GetPayrollsAsync(
        PayrollListQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<PayrollAccessResult> GetPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollFormOptionsResult> GetFormOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> CreatePayrollAsync(
        CreatePayrollRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> UpdatePayrollAsync(
        Guid id,
        UpdatePayrollRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollDuplicateResult> DuplicatePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollEntryPreviewResult> PreviewEntryAsync(
        Guid payrollId,
        Guid entryId,
        PreviewPayrollEntryRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> SubmitPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> ApprovePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> RejectPayrollAsync(
        Guid id,
        RejectPayrollRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> RecalculatePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollDeleteResult> DeletePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> PayPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> ApproveEntryAsync(
        Guid payrollId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> PayEntryAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryPaidRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> SetEntryNfAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryNfRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PayrollMutationResult> AddCollaboratorEntryAsync(
        Guid payrollId,
        AddCollaboratorEntryRequestDto request,
        CancellationToken cancellationToken = default);
}
