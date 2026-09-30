using Core.Application.Payrolls;
using Core.Domain;

namespace Core.Application.Finance;

public sealed record FinanceSummaryFilters(
    int? Month,
    int? Year,
    Guid? DepartmentId,
    Guid? ProjectId);

public sealed record FinanceDepartmentOption(Guid Id, string Name);

public sealed record FinanceProjectOption(Guid Id, string Name);

public sealed record FinanceFilterOptionsResponse(
    IReadOnlyList<FinanceDepartmentOption> Departments,
    IReadOnlyList<FinanceProjectOption> Projects);

public sealed record FinanceEntryResponse(
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
    IReadOnlyList<PayrollProjectTotalResponse> ProjectTotals);

public sealed record FinancePayrollGroupResponse(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    PayrollStatus Status,
    decimal GrossTotal,
    decimal PlatformTotal,
    decimal AmountToReceive,
    decimal PaidAmount,
    int PaidCount,
    int EntryCount,
    PayrollAllowedActionsResponse AllowedActions,
    IReadOnlyList<FinanceEntryResponse> Entries);

public sealed record FinanceSummaryResponse(
    IReadOnlyList<FinancePayrollGroupResponse> Payrolls,
    FinanceFilterOptionsResponse FilterOptions);
