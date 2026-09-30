using Core.Domain;

namespace Core.Application.Finance;

public static class FinanceEntryVisibility
{
    public static bool IsEntryVisible(PayrollStatus payrollStatus, bool isApproved) =>
        payrollStatus is PayrollStatus.Approved or PayrollStatus.Paid || isApproved;

    public static bool PayrollHasFinanceContent(PayrollStatus payrollStatus, IEnumerable<bool> entryApprovalFlags) =>
        payrollStatus is PayrollStatus.Approved or PayrollStatus.Paid
        || entryApprovalFlags.Any(isApproved => isApproved);
}
