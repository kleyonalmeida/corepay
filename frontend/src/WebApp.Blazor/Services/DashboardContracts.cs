namespace WebApp.Blazor.Services;

public enum DashboardApiStatus
{
    Success,
    ValidationError,
    Error
}

public sealed record DashboardQuery(int? Month = null, int? Year = null);

public sealed record DashboardPayrollStatsDto(
    int TotalPayrolls,
    int AwaitingApproval,
    int Approved,
    int Rejected,
    decimal TotalToPay,
    decimal TotalPaid);

public sealed record DashboardRecentPayrollDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    string Status,
    decimal TotalAmount,
    int EntryCount);

public sealed record DashboardDto(
    int Month,
    int Year,
    DashboardPayrollStatsDto? PayrollStats,
    int? ActiveCollaborators,
    IReadOnlyList<DashboardRecentPayrollDto>? RecentPayrolls);

public sealed record DashboardResult(
    DashboardApiStatus Status,
    DashboardDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);
