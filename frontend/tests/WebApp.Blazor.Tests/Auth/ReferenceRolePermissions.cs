using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

internal static class ReferenceRolePermissions
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Map { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Admin"] = AppPermissions.All,
            ["Director"] =
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
            ["Financial"] =
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
            ["Manager"] =
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
            ["User"] = []
        };
}
