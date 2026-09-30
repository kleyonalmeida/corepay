namespace Core.Domain;

public static class ProjectCostCategoryRules
{
    private static readonly HashSet<ProjectCostCategory> EntryCategories =
    [
        ProjectCostCategory.Plataforma,
        ProjectCostCategory.Igaming,
        ProjectCostCategory.DevolucoesReembolso
    ];

    private static readonly HashSet<ProjectCostCategory> ExitCategories =
    [
        ProjectCostCategory.FolhaPagamento,
        ProjectCostCategory.Trafego,
        ProjectCostCategory.Acoes,
        ProjectCostCategory.RecargasBanca,
        ProjectCostCategory.Viagens,
        ProjectCostCategory.Imposto,
        ProjectCostCategory.Reembolso,
        ProjectCostCategory.DespesaAlimentar,
        ProjectCostCategory.MoveisEquipamentos,
        ProjectCostCategory.Experts,
        ProjectCostCategory.Reforma,
        ProjectCostCategory.Aeronave,
        ProjectCostCategory.Administrativa,
        ProjectCostCategory.PlataformasDigitais,
        ProjectCostCategory.FestasEventos
    ];

    public static bool IsValidForType(ProjectCostType type, ProjectCostCategory category) =>
        type switch
        {
            ProjectCostType.Entrada => EntryCategories.Contains(category),
            ProjectCostType.Saida => ExitCategories.Contains(category),
            _ => false
        };

    public static IReadOnlyList<ProjectCostCategory> GetCategoriesForType(ProjectCostType type) =>
        type switch
        {
            ProjectCostType.Entrada => EntryCategories.OrderBy(c => (int)c).ToList(),
            ProjectCostType.Saida => ExitCategories.OrderBy(c => (int)c).ToList(),
            _ => []
        };
}
