namespace Core.Auth;

public static class RolePermissionMap
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ReferenceRolePermissions { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [AppRoles.Admin] =
            [
                AppPermissions.DepartmentsRead,
                AppPermissions.DepartmentsWrite,
                AppPermissions.CareerLevelsRead,
                AppPermissions.CareerLevelsWrite,
                AppPermissions.ProjectsRead,
                AppPermissions.ProjectsWrite,
                AppPermissions.CollaboratorsRead,
                AppPermissions.CollaboratorsWrite,
                AppPermissions.PaymentMethodsRead,
                AppPermissions.PaymentMethodsWrite,
                AppPermissions.PayrollsRead,
                AppPermissions.PayrollsWrite,
                AppPermissions.PayrollsApprove,
                AppPermissions.PayrollsPay,
                AppPermissions.PayrollsDelete,
                AppPermissions.FinanceRead,
                AppPermissions.RevenuesRead,
                AppPermissions.RevenuesWrite,
                AppPermissions.AnalystMetricsRead,
                AppPermissions.AnalystMetricsWrite,
                AppPermissions.ReportsRead,
                AppPermissions.TrafficRead,
                AppPermissions.TrafficWrite,
                AppPermissions.CashflowRead,
                AppPermissions.CashflowWrite,
                AppPermissions.UsersRead,
                AppPermissions.UsersWrite,
                AppPermissions.RolesRead,
                AppPermissions.RolesWrite,
                AppPermissions.PermissionsRead,
                AppPermissions.PermissionsWrite
            ],
            [AppRoles.Director] =
            [
                AppPermissions.DepartmentsRead,
                AppPermissions.CareerLevelsRead,
                AppPermissions.ProjectsRead,
                AppPermissions.CollaboratorsRead,
                AppPermissions.PaymentMethodsRead,
                AppPermissions.PaymentMethodsWrite,
                AppPermissions.PayrollsRead,
                AppPermissions.PayrollsApprove,
                AppPermissions.FinanceRead,
                AppPermissions.RevenuesRead,
                AppPermissions.AnalystMetricsRead,
                AppPermissions.ReportsRead,
                AppPermissions.TrafficRead,
                AppPermissions.CashflowRead
            ],
            [AppRoles.Financial] =
            [
                AppPermissions.DepartmentsRead,
                AppPermissions.CareerLevelsRead,
                AppPermissions.ProjectsRead,
                AppPermissions.CollaboratorsRead,
                AppPermissions.PaymentMethodsRead,
                AppPermissions.PayrollsRead,
                AppPermissions.PayrollsWrite,
                AppPermissions.PayrollsPay,
                AppPermissions.FinanceRead,
                AppPermissions.RevenuesRead,
                AppPermissions.RevenuesWrite,
                AppPermissions.ReportsRead,
                AppPermissions.CashflowRead,
                AppPermissions.CashflowWrite
            ],
            [AppRoles.Manager] =
            [
                AppPermissions.DepartmentsRead,
                AppPermissions.CareerLevelsRead,
                AppPermissions.ProjectsRead,
                AppPermissions.CollaboratorsRead,
                AppPermissions.CollaboratorsWrite,
                AppPermissions.PayrollsRead,
                AppPermissions.PayrollsWrite,
                AppPermissions.RevenuesRead,
                AppPermissions.RevenuesWrite,
                AppPermissions.AnalystMetricsRead,
                AppPermissions.AnalystMetricsWrite
            ],
            [AppRoles.User] = []
        };
}
