using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class FinanceErrorMessages
{
    public static string ForSummary(FinanceSummaryResult result) =>
        result.Status switch
        {
            FinanceApiStatus.Forbidden => "Você não tem permissão para acessar o financeiro.",
            FinanceApiStatus.Error => "Não foi possível carregar os dados financeiros. Tente novamente.",
            _ => "Não foi possível carregar os dados financeiros."
        };
}
