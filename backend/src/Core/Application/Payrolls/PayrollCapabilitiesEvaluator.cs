using Core.Auth;
using Core.Domain;

namespace Core.Application.Payrolls;

public static class PayrollCapabilitiesEvaluator
{
    public static PayrollAllowedActionsResponse Evaluate(
        PayrollStatus status,
        PayrollAccessContext access)
    {
        var isSuperAdmin = access.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        var hasApprove = HasPermission(access, AppPermissions.PayrollsApprove, isSuperAdmin);
        var hasWrite = HasPermission(access, AppPermissions.PayrollsWrite, isSuperAdmin);
        var hasPay = HasPermission(access, AppPermissions.PayrollsPay, isSuperAdmin);
        var hasDelete = HasPermission(access, AppPermissions.PayrollsDelete, isSuperAdmin);

        var isEditable = status is PayrollStatus.Draft or PayrollStatus.Rejected;
        var isPendingApproval = status == PayrollStatus.PendingApproval;
        var isApproved = status == PayrollStatus.Approved;
        var canPayByStatus = !isPendingApproval;

        return new PayrollAllowedActionsResponse(
            ApprovePayroll: hasApprove && isPendingApproval,
            RejectPayroll: hasApprove && isPendingApproval,
            ApproveEntry: hasApprove,
            Edit: hasWrite && isEditable,
            PayPayroll: hasPay && canPayByStatus,
            PayEntry: hasPay && canPayByStatus,
            ToggleNf: hasPay && canPayByStatus,
            PostApprovalAdjustments: hasPay && isApproved,
            Delete: hasDelete,
            Recalculate: hasWrite && isEditable,
            AddCollaborator: hasWrite && !isPendingApproval);
    }

    private static bool HasPermission(
        PayrollAccessContext access,
        string permissionKey,
        bool isSuperAdmin)
    {
        if (isSuperAdmin)
        {
            return true;
        }

        var permissions = access.Permissions ?? [];
        return permissions.Contains(permissionKey, StringComparer.OrdinalIgnoreCase);
    }
}
