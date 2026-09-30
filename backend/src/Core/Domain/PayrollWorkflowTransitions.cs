using BuildingBlocks.Results;

namespace Core.Domain;

public static class PayrollWorkflowTransitions
{
    public static Result ValidateCanApprove(PayrollStatus status) =>
        status == PayrollStatus.PendingApproval
            ? Result.Success()
            : Result.Failure(
                Error.Conflict(
                    "payrolls.status_not_approvable",
                    "Payroll can only be approved while pending approval."));

    public static Result ValidateCanReject(PayrollStatus status) =>
        status == PayrollStatus.PendingApproval
            ? Result.Success()
            : Result.Failure(
                Error.Conflict(
                    "payrolls.status_not_rejectable",
                    "Payroll can only be rejected while pending approval."));

    public static Result ValidateRejectionComment(string? rejectionComment) =>
        string.IsNullOrWhiteSpace(rejectionComment)
            ? Result.Failure(
                Error.Validation(
                    "payrolls.rejection_comment_required",
                    "Rejection comment is required."))
            : Result.Success();

    public static Result ValidateCanPay(PayrollStatus status) =>
        status == PayrollStatus.PendingApproval
            ? Result.Failure(
                Error.Conflict(
                    "payrolls.pending_approval_cannot_pay",
                    "Payroll cannot be paid while pending approval."))
            : Result.Success();

    public static Result ValidateCanRecalculate(PayrollStatus status) =>
        status is PayrollStatus.Draft or PayrollStatus.Rejected
            ? Result.Success()
            : Result.Failure(
                Error.Conflict(
                    "payrolls.status_not_editable",
                    "Payroll cannot be edited in its current status."));

    public static void ApprovePayroll(Payroll payroll, string approvedBy, DateTimeOffset approvedAt)
    {
        payroll.Status = PayrollStatus.Approved;
        payroll.ApprovedBy = approvedBy;
        payroll.ApprovedAt = approvedAt;
        payroll.RejectionComment = null;

        foreach (var entry in payroll.Entries)
        {
            entry.IsApproved = true;
        }
    }

    public static void RejectPayroll(Payroll payroll, string rejectionComment)
    {
        payroll.Status = PayrollStatus.Rejected;
        payroll.RejectionComment = rejectionComment.Trim();
        payroll.ApprovedBy = null;
        payroll.ApprovedAt = null;
    }

    public static void SyncPayrollPaidStatus(Payroll payroll)
    {
        if (payroll.Entries.Count == 0)
        {
            return;
        }

        var allPaid = payroll.Entries.All(entry => entry.IsPaid);
        if (allPaid && payroll.Status is PayrollStatus.Approved or PayrollStatus.Paid)
        {
            payroll.Status = PayrollStatus.Paid;
            return;
        }

        if (payroll.Status == PayrollStatus.Paid && !allPaid)
        {
            payroll.Status = PayrollStatus.Approved;
        }
    }

    public static void PayAllEntries(Payroll payroll)
    {
        foreach (var entry in payroll.Entries)
        {
            entry.IsPaid = true;
        }

        SyncPayrollPaidStatus(payroll);
    }
}
