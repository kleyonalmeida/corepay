using WebApp.Blazor.Auth;
using WebApp.Blazor.Components.Ui;

namespace WebApp.Blazor.Layout;

public sealed record ShellNavItem(
    string Href,
    string Label,
    string Title,
    IconKind Icon,
    string? Policy = null);

public static class ShellNavigation
{
    public const string PayrollsDisplayName = "Folhas de Pagamentos";

    public static IReadOnlyList<ShellNavItem> MainItems { get; } =
    [
        new("/", "Dashboard", "Dashboard", IconKind.LayoutDashboard),
        new("/payrolls", PayrollsDisplayName, PayrollsDisplayName, IconKind.FileText, AppPolicies.PayrollsRead),
        new("/collaborators", "Colaboradores", "Colaboradores", IconKind.Users, AppPolicies.CollaboratorsRead),
        new("/analyst-metrics", "Métricas de analista", "Métricas de analista", IconKind.ChartColumn, AppPolicies.AnalystMetricsRead),
        new("/project-revenues", "Faturamento", "Faturamento", IconKind.TrendingUp, AppPolicies.RevenuesRead),
        new("/traffic-investment", "Investimento Tráfego", "Investimento Tráfego", IconKind.Target, AppPolicies.TrafficRead),
        new("/financial", "Financeiro", "Financeiro", IconKind.DollarSign, AppPolicies.FinanceRead),
        new("/cashflow", "Fluxo de Caixa", "Fluxo de Caixa", IconKind.Building2, AppPolicies.CashflowRead),
        new("/reports", "Relatórios", "Relatórios", IconKind.BarChart3, AppPolicies.ReportsRead),
        new("/settings", "Configurações", "Configurações", IconKind.Settings, AppPolicies.MasterData)
    ];

    public static IReadOnlyList<ShellNavItem> AdminItems { get; } =
    [
        new("/admin/users", "Usuários", "Usuários", IconKind.Users, AppPolicies.UsersRead),
        new("/admin/roles", "Papéis", "Papéis", IconKind.Settings, AppPolicies.RolesRead)
    ];

    public static string? GetTitleForPath(string path)
    {
        var normalized = NormalizePath(path);
        var all = MainItems.Concat(AdminItems);

        var exact = all.FirstOrDefault(item => NormalizePath(item.Href) == normalized);
        if (exact is not null)
        {
            return exact.Title;
        }

        if (normalized == "/notifications")
        {
            return "Notificações";
        }

        if (normalized.StartsWith("/payrolls", StringComparison.OrdinalIgnoreCase))
        {
            return PayrollsDisplayName;
        }

        return null;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var value = path.Split('?', '#')[0];
        if (value.Length > 1 && value.EndsWith('/'))
        {
            value = value.TrimEnd('/');
        }

        return value;
    }
}
