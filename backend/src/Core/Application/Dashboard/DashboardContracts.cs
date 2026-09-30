using Core.Domain;

namespace Core.Application.Dashboard;

public sealed record DashboardFilters(int? Month, int? Year);

public sealed record DashboardPayrollStatsResponse(
    int TotalPayrolls,
    int AwaitingApproval,
    int Approved,
    int Rejected,
    decimal TotalToPay,
    decimal TotalPaid);

public sealed record DashboardRecentPayrollResponse(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    PayrollStatus Status,
    decimal TotalAmount,
    int EntryCount);

public sealed record DashboardResponse(
    int Month,
    int Year,
    DashboardPayrollStatsResponse? PayrollStats,
    int? ActiveCollaborators,
    IReadOnlyList<DashboardRecentPayrollResponse>? RecentPayrolls);
