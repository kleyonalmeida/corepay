namespace WebApp.Blazor.Auth;

public static class AppPolicies
{
    public const string PayrollsRead = "permission:payrolls.read";
    public const string PayrollsWrite = "permission:payrolls.write";
    public const string PayrollsApprove = "permission:payrolls.approve";
    public const string PayrollsPay = "permission:payrolls.pay";
    public const string PayrollsDelete = "permission:payrolls.delete";
    public const string CollaboratorsRead = "permission:collaborators.read";
    public const string CollaboratorsWrite = "permission:collaborators.write";
    public const string RevenuesRead = "permission:revenues.read";
    public const string RevenuesWrite = "permission:revenues.write";
    public const string AnalystMetricsRead = "permission:analystmetrics.read";
    public const string AnalystMetricsWrite = "permission:analystmetrics.write";
    public const string TrafficRead = "permission:traffic.read";
    public const string TrafficWrite = "permission:traffic.write";
    public const string FinanceRead = "permission:finance.read";
    public const string CashflowRead = "permission:cashflow.read";
    public const string CashflowWrite = "permission:cashflow.write";
    public const string PaymentMethodsWrite = "permission:paymentmethods.write";
    public const string ReportsRead = "permission:reports.read";
    public const string UsersRead = "permission:users.read";
    public const string UsersWrite = "permission:users.write";
    public const string RolesRead = "permission:roles.read";
    public const string RolesWrite = "permission:roles.write";
    public const string PermissionsRead = "permission:permissions.read";
    public const string DepartmentsWrite = "permission:departments.write";
    public const string CareerLevelsWrite = "permission:careerlevels.write";
    public const string ProjectsWrite = "permission:projects.write";

    public const string MasterData =
        "permission-any:departments.write,careerlevels.write,projects.write,paymentmethods.write";
}
