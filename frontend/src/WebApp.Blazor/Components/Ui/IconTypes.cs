namespace WebApp.Blazor.Components.Ui;

/// <summary>
/// Catálogo tipado de ícones Lucide permitidos (IDENTIDADE_VISUAL.md §5).
/// </summary>
public enum IconKind
{
    LayoutDashboard,
    FileText,
    Users,
    TrendingUp,
    Target,
    DollarSign,
    Building2,
    BarChart3,
    ChartColumn,
    Settings,
    LogOut,
    Sun,
    Moon,
    Bell,
    PanelLeftClose,
    PanelLeftOpen,
    Plus,
    Pencil,
    Trash2,
    Search,
    ExternalLink,
    Check,
    X,
    UserCheck,
    UserX,
    CheckCircle2,
    Wallet,
    Calendar,
    ChevronRight,
    Download,
    Eye,
    EyeOff
}

/// <summary>
/// Tamanhos semânticos de ícone conforme IDENTIDADE_VISUAL.md §5.
/// </summary>
public enum IconSize
{
    /// <summary>16×16 — padrão (botões, nav, stat card).</summary>
    Default = 16,

    /// <summary>20×20 — topbar (tema, notificações).</summary>
    Topbar = 20,

    /// <summary>28×28 — auth hero dentro de quadrado 56×56.</summary>
    AuthHero = 28,

    /// <summary>32×32 — empty state.</summary>
    EmptyState = 32
}
