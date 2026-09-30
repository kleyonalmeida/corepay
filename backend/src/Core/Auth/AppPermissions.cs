namespace Core.Auth;

public static class AppPermissions
{
    public const string DepartmentsRead = "departments.read";
    public const string DepartmentsWrite = "departments.write";
    public const string CareerLevelsRead = "careerlevels.read";
    public const string CareerLevelsWrite = "careerlevels.write";
    public const string ProjectsRead = "projects.read";
    public const string ProjectsWrite = "projects.write";
    public const string CollaboratorsRead = "collaborators.read";
    public const string CollaboratorsWrite = "collaborators.write";
    public const string PaymentMethodsRead = "paymentmethods.read";
    public const string PaymentMethodsWrite = "paymentmethods.write";
    public const string PayrollsRead = "payrolls.read";
    public const string PayrollsWrite = "payrolls.write";
    public const string PayrollsApprove = "payrolls.approve";
    public const string PayrollsPay = "payrolls.pay";
    public const string PayrollsDelete = "payrolls.delete";
    public const string FinanceRead = "finance.read";
    public const string RevenuesRead = "revenues.read";
    public const string RevenuesWrite = "revenues.write";
    public const string AnalystMetricsRead = "analystmetrics.read";
    public const string AnalystMetricsWrite = "analystmetrics.write";
    public const string ReportsRead = "reports.read";
    public const string TrafficRead = "traffic.read";
    public const string TrafficWrite = "traffic.write";
    public const string CashflowRead = "cashflow.read";
    public const string CashflowWrite = "cashflow.write";
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string PermissionsRead = "permissions.read";
    public const string PermissionsWrite = "permissions.write";

    public static readonly IReadOnlyList<string> All =
    [
        DepartmentsRead,
        DepartmentsWrite,
        CareerLevelsRead,
        CareerLevelsWrite,
        ProjectsRead,
        ProjectsWrite,
        CollaboratorsRead,
        CollaboratorsWrite,
        PaymentMethodsRead,
        PaymentMethodsWrite,
        PayrollsRead,
        PayrollsWrite,
        PayrollsApprove,
        PayrollsPay,
        PayrollsDelete,
        FinanceRead,
        RevenuesRead,
        RevenuesWrite,
        AnalystMetricsRead,
        AnalystMetricsWrite,
        ReportsRead,
        TrafficRead,
        TrafficWrite,
        CashflowRead,
        CashflowWrite,
        UsersRead,
        UsersWrite,
        RolesRead,
        RolesWrite,
        PermissionsRead,
        PermissionsWrite
    ];
}
