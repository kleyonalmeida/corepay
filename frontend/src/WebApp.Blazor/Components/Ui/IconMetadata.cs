namespace WebApp.Blazor.Components.Ui;

internal static class IconMetadata
{
    private static readonly IReadOnlyDictionary<IconKind, string> LucideNames = new Dictionary<IconKind, string>
    {
        [IconKind.LayoutDashboard] = "layout-dashboard",
        [IconKind.FileText] = "file-text",
        [IconKind.Users] = "users",
        [IconKind.TrendingUp] = "trending-up",
        [IconKind.Target] = "target",
        [IconKind.DollarSign] = "dollar-sign",
        [IconKind.Building2] = "building-2",
        [IconKind.BarChart3] = "chart-bar",
        [IconKind.ChartColumn] = "chart-column",
        [IconKind.Settings] = "settings",
        [IconKind.LogOut] = "log-out",
        [IconKind.Sun] = "sun",
        [IconKind.Moon] = "moon",
        [IconKind.Bell] = "bell",
        [IconKind.PanelLeftClose] = "panel-left-close",
        [IconKind.PanelLeftOpen] = "panel-left-open",
        [IconKind.Plus] = "plus",
        [IconKind.Pencil] = "pencil",
        [IconKind.Trash2] = "trash-2",
        [IconKind.Search] = "search",
        [IconKind.ExternalLink] = "external-link",
        [IconKind.Check] = "check",
        [IconKind.X] = "x",
        [IconKind.UserCheck] = "user-check",
        [IconKind.UserX] = "user-x",
        [IconKind.CheckCircle2] = "circle-check",
        [IconKind.Wallet] = "wallet",
        [IconKind.Calendar] = "calendar",
        [IconKind.ChevronRight] = "chevron-right",
        [IconKind.Download] = "download",
        [IconKind.Eye] = "eye",
        [IconKind.EyeOff] = "eye-off",
        [IconKind.AlertTriangle] = "triangle-alert",
        [IconKind.ArrowUp] = "arrow-up",
        [IconKind.ArrowDown] = "arrow-down",
        [IconKind.Minus] = "minus"
    };

    public static string GetLucideName(IconKind kind) =>
        LucideNames.TryGetValue(kind, out var name)
            ? name
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Ícone não catalogado.");

    public static IconKind ForStatCardTone(SemanticTone tone) => tone switch
    {
        SemanticTone.Blue => IconKind.FileText,
        SemanticTone.Yellow => IconKind.Bell,
        SemanticTone.Emerald => IconKind.CheckCircle2,
        SemanticTone.Red => IconKind.X,
        SemanticTone.Orange => IconKind.Wallet,
        SemanticTone.Purple => IconKind.Users,
        _ => IconKind.DollarSign
    };
}
