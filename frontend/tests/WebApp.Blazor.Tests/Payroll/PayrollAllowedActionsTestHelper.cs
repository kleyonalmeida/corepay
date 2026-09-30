namespace WebApp.Blazor.Tests.Payroll;

internal static class PayrollAllowedActionsTestHelper
{
    public static object ForManagerDraft() =>
        Create(
            edit: true,
            recalculate: true);

    public static object ForPendingApprovalDirector() =>
        Create(
            approvePayroll: true,
            rejectPayroll: true,
            approveEntry: true);

    public static object ForPendingApprovalFinancial() =>
        Create();

    public static object ForApprovedAdmin() =>
        Create(
            approveEntry: true,
            payPayroll: true,
            payEntry: true,
            toggleNf: true,
            postApprovalAdjustments: true,
            delete: true);

    public static object ForRejectedManager() =>
        Create(
            edit: true,
            recalculate: true);

    public static object ForApprovedFinancial() =>
        Create(
            payPayroll: true,
            payEntry: true,
            toggleNf: true,
            postApprovalAdjustments: true,
            addCollaborator: true);

    public static object ForApprovedDirector() =>
        Create(
            approveEntry: true);

    public static object Create(
        bool approvePayroll = false,
        bool rejectPayroll = false,
        bool approveEntry = false,
        bool edit = false,
        bool payPayroll = false,
        bool payEntry = false,
        bool toggleNf = false,
        bool postApprovalAdjustments = false,
        bool delete = false,
        bool recalculate = false,
        bool addCollaborator = false) =>
        new
        {
            approvePayroll,
            rejectPayroll,
            approveEntry,
            edit,
            payPayroll,
            payEntry,
            toggleNf,
            postApprovalAdjustments,
            delete,
            recalculate,
            addCollaborator
        };
}
