using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class CashflowCategoryLabels
{
    public static string GetLabel(CashflowEntryCategory category) =>
        category switch
        {
            CashflowEntryCategory.Plataforma => "Plataforma",
            CashflowEntryCategory.Igaming => "Igaming",
            CashflowEntryCategory.DevolucoesReembolso => "Devoluções e reembolso",
            CashflowEntryCategory.FolhaPagamento => "Folha de pagamento",
            CashflowEntryCategory.Trafego => "Tráfego",
            CashflowEntryCategory.Acoes => "Ações",
            CashflowEntryCategory.RecargasBanca => "Recargas de banca",
            CashflowEntryCategory.Viagens => "Viagens",
            CashflowEntryCategory.Imposto => "Imposto",
            CashflowEntryCategory.Reembolso => "Reembolso",
            CashflowEntryCategory.DespesaAlimentar => "Despesa alimentar",
            CashflowEntryCategory.MoveisEquipamentos => "Móveis e equipamentos",
            CashflowEntryCategory.Experts => "Experts",
            CashflowEntryCategory.Reforma => "Reforma",
            CashflowEntryCategory.Aeronave => "Aeronave",
            CashflowEntryCategory.Administrativa => "Administrativa",
            CashflowEntryCategory.PlataformasDigitais => "Plataformas digitais",
            CashflowEntryCategory.FestasEventos => "Festas e eventos",
            _ => category.ToString()
        };

    public static IReadOnlyList<CashflowEntryCategory> GetCategoriesForType(CashflowEntryType type) =>
        type switch
        {
            CashflowEntryType.Entrada =>
            [
                CashflowEntryCategory.Plataforma,
                CashflowEntryCategory.Igaming,
                CashflowEntryCategory.DevolucoesReembolso
            ],
            CashflowEntryType.Saida =>
            [
                CashflowEntryCategory.FolhaPagamento,
                CashflowEntryCategory.Trafego,
                CashflowEntryCategory.Acoes,
                CashflowEntryCategory.RecargasBanca,
                CashflowEntryCategory.Viagens,
                CashflowEntryCategory.Imposto,
                CashflowEntryCategory.Reembolso,
                CashflowEntryCategory.DespesaAlimentar,
                CashflowEntryCategory.MoveisEquipamentos,
                CashflowEntryCategory.Experts,
                CashflowEntryCategory.Reforma,
                CashflowEntryCategory.Aeronave,
                CashflowEntryCategory.Administrativa,
                CashflowEntryCategory.PlataformasDigitais,
                CashflowEntryCategory.FestasEventos
            ],
            _ => []
        };

    public static string GetTypeLabel(CashflowEntryType type) =>
        type switch
        {
            CashflowEntryType.Entrada => "Entrada",
            CashflowEntryType.Saida => "Saída",
            _ => type.ToString()
        };
}
