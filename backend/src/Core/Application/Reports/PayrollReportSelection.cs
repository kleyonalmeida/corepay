using Core.Application.Finance;
using Core.Domain;

namespace Core.Application.Reports;

public static class PayrollReportSelection
{
    public static int StatusPriority(PayrollStatus status) =>
        status switch
        {
            PayrollStatus.Paid => 4,
            PayrollStatus.Approved => 3,
            PayrollStatus.PendingApproval => 2,
            _ => 1
        };

    public static bool IsPayrollEligible(Payroll payroll) =>
        FinanceEntryVisibility.PayrollHasFinanceContent(
            payroll.Status,
            payroll.Entries.Select(entry => entry.IsApproved));

    public static IEnumerable<Payroll> SelectWinningPayrolls(IEnumerable<Payroll> candidates) =>
        candidates
            .Where(IsPayrollEligible)
            .GroupBy(p => (p.DepartmentId, p.Month, p.Year))
            .Select(group => group
                .OrderByDescending(p => StatusPriority(p.Status))
                .ThenByDescending(p => p.Id)
                .First());
}
