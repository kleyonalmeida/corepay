using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Components.Admin;

public sealed record RoleAccessDescriptor(
    string PermissionKey,
    string Area,
    string Label,
    string Route,
    string Description);

public sealed record RoleFormSubmit(string Name, IReadOnlyList<string> PermissionKeys);

public static class RoleAccessCatalog
{
    public static IReadOnlyList<RoleAccessDescriptor> Items { get; } =
    [
        new(AppPermissions.PayrollsRead, "Folhas", "Visualizar", "/payrolls", "Lista, detalhes e dados das folhas."),
        new(AppPermissions.PayrollsWrite, "Folhas", "Criar e editar", "/payrolls", "Criação, edição e submissão de folhas."),
        new(AppPermissions.PayrollsApprove, "Folhas", "Aprovar e rejeitar", "/payrolls", "Transições de aprovação da folha."),
        new(AppPermissions.PayrollsPay, "Folhas", "Pagar e ajustar", "/financial", "Pagamento e ajustes financeiros permitidos."),
        new(AppPermissions.PayrollsDelete, "Folhas", "Excluir", "/payrolls", "Exclusão de folhas."),
        new(AppPermissions.CollaboratorsRead, "Colaboradores", "Visualizar", "/collaborators", "Consulta de colaboradores."),
        new(AppPermissions.CollaboratorsWrite, "Colaboradores", "Criar e editar", "/collaborators", "Manutenção de colaboradores."),
        new(AppPermissions.AnalystMetricsRead, "Métricas", "Visualizar", "/analyst-metrics", "Consulta de métricas de analistas."),
        new(AppPermissions.AnalystMetricsWrite, "Métricas", "Criar e editar", "/analyst-metrics", "Manutenção de métricas de analistas."),
        new(AppPermissions.RevenuesRead, "Faturamento", "Visualizar", "/project-revenues", "Consulta do faturamento por projeto."),
        new(AppPermissions.RevenuesWrite, "Faturamento", "Criar e editar", "/project-revenues", "Manutenção do faturamento."),
        new(AppPermissions.TrafficRead, "Tráfego", "Visualizar", "/traffic-investment", "Consulta de investimentos e aportes."),
        new(AppPermissions.TrafficWrite, "Tráfego", "Criar e editar", "/traffic-investment", "Manutenção de investimentos e aportes."),
        new(AppPermissions.FinanceRead, "Financeiro", "Visualizar", "/financial", "Consulta da área financeira."),
        new(AppPermissions.CashflowRead, "Fluxo de caixa", "Visualizar", "/cashflow", "Consulta dos lançamentos e relatórios."),
        new(AppPermissions.CashflowWrite, "Fluxo de caixa", "Criar, editar e excluir", "/cashflow", "Manutenção dos lançamentos."),
        new(AppPermissions.ReportsRead, "Relatórios", "Visualizar e exportar", "/reports", "Consulta e exportação de relatórios."),
        new(AppPermissions.DepartmentsRead, "Cadastros", "Consultar setores", "/settings", "Leitura do catálogo de setores."),
        new(AppPermissions.DepartmentsWrite, "Cadastros", "Gerenciar setores", "/settings", "Manutenção de setores."),
        new(AppPermissions.CareerLevelsRead, "Cadastros", "Consultar níveis", "/settings", "Leitura dos níveis de carreira."),
        new(AppPermissions.CareerLevelsWrite, "Cadastros", "Gerenciar níveis", "/settings", "Manutenção dos níveis de carreira."),
        new(AppPermissions.ProjectsRead, "Cadastros", "Consultar projetos", "/settings", "Leitura do catálogo de projetos."),
        new(AppPermissions.ProjectsWrite, "Cadastros", "Gerenciar projetos", "/settings", "Manutenção de projetos."),
        new(AppPermissions.PaymentMethodsRead, "Cadastros", "Consultar formas de pagamento", "/settings", "Leitura das formas de pagamento."),
        new(AppPermissions.PaymentMethodsWrite, "Cadastros", "Gerenciar formas de pagamento", "/settings", "Manutenção das formas de pagamento."),
        new(AppPermissions.UsersRead, "Administração", "Visualizar usuários", "/admin/users", "Consulta de usuários e seus vínculos."),
        new(AppPermissions.UsersWrite, "Administração", "Gerenciar usuários", "/admin/users", "Criação, edição e exclusão de usuários."),
        new(AppPermissions.RolesRead, "Administração", "Visualizar papéis", "/admin/roles", "Consulta de papéis e acessos."),
        new(AppPermissions.RolesWrite, "Administração", "Gerenciar papéis", "/admin/roles", "Criação e edição de papéis; exige SuperAdmin."),
        new(AppPermissions.PermissionsRead, "Administração", "Consultar catálogo de permissões", "/admin/roles", "Necessária para configurar acessos."),
        new(AppPermissions.PermissionsWrite, "Administração", "Gerenciar catálogo de permissões", "API", "Permite manter permissões técnicas; exige SuperAdmin.")
    ];

    public static RoleAccessDescriptor For(PermissionDto permission) =>
        ForKey(permission.Key)
        ?? new(permission.Key, "Outros", permission.Description, "API", permission.Description);

    public static RoleAccessDescriptor? ForKey(string key) =>
        Items.FirstOrDefault(item => string.Equals(item.PermissionKey, key, StringComparison.OrdinalIgnoreCase));
}
