namespace WebApp.Blazor.Services;

public enum FinanceApiStatus
{
    Success,
    Forbidden,
    Error
}

public sealed record FinanceSummaryQuery(
    int? Month = null,
    int? Year = null,
    Guid? DepartmentId = null,
    Guid? ProjectId = null);

public sealed record FinanceDepartmentOptionDto(Guid Id, string Name);

public sealed record FinanceProjectOptionDto(Guid Id, string Name);

public sealed record FinanceFilterOptionsDto(
    IReadOnlyList<FinanceDepartmentOptionDto> Departments,
    IReadOnlyList<FinanceProjectOptionDto> Projects);

public sealed record FinanceEntryDto(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    string? CareerLevelName,
    string? PixKey,
    decimal TotalAmount,
    decimal PlatformTotal,
    decimal AmountToReceive,
    bool IsApproved,
    bool IsPaid,
    bool NfSent,
    IReadOnlyList<PayrollProjectTotalDto>? ProjectTotals = null);

public sealed record FinanceAllowedActionsDto(
    bool ApprovePayroll,
    bool RejectPayroll,
    bool ApproveEntry,
    bool Edit,
    bool PayPayroll,
    bool PayEntry,
    bool ToggleNf,
    bool PostApprovalAdjustments,
    bool Delete,
    bool Recalculate,
    bool AddCollaborator);

public sealed record FinancePayrollGroupDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    string Status,
    decimal GrossTotal,
    decimal PlatformTotal,
    decimal AmountToReceive,
    decimal PaidAmount,
    int PaidCount,
    int EntryCount,
    FinanceAllowedActionsDto AllowedActions,
    IReadOnlyList<FinanceEntryDto> Entries);

public sealed record FinanceSummaryDto(
    IReadOnlyList<FinancePayrollGroupDto> Payrolls,
    FinanceFilterOptionsDto FilterOptions);

public sealed record FinanceSummaryResult(
    FinanceApiStatus Status,
    FinanceSummaryDto? Summary = null,
    string? ErrorCode = null,
    string? Message = null);
