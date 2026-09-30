using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class TrafficInvestmentErrorMessages
{
    public static string ForMutation(TrafficInvestmentMutationResult result) =>
        result.Status switch
        {
            TrafficInvestmentApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            TrafficInvestmentApiStatus.Conflict =>
                "Já existe investimento de tráfego para este projeto na competência selecionada.",
            TrafficInvestmentApiStatus.NotFound => "Investimento de tráfego não encontrado.",
            TrafficInvestmentApiStatus.Forbidden => "Acesso negado.",
            TrafficInvestmentApiStatus.Error => "Não foi possível salvar o investimento.",
            _ => "Não foi possível salvar o investimento."
        };

    public static string ForList(TrafficInvestmentListResult result) =>
        result.Status switch
        {
            TrafficInvestmentApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            TrafficInvestmentApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os investimentos."
        };

    public static string ForProjectDepositMutation(TrafficProjectDepositMutationResult result) =>
        result.Status switch
        {
            TrafficInvestmentApiStatus.ValidationError => TranslateDepositValidation(result.ErrorCode, result.Message),
            TrafficInvestmentApiStatus.Forbidden => "Acesso negado.",
            TrafficInvestmentApiStatus.Error => "Não foi possível salvar o aporte.",
            _ => "Não foi possível salvar o aporte."
        };

    public static string ForProjectDepositList(TrafficProjectDepositListResult result) =>
        result.Status switch
        {
            TrafficInvestmentApiStatus.ValidationError => TranslateDepositValidation(result.ErrorCode, result.Message),
            TrafficInvestmentApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os aportes."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "trafficinvestments.project_not_found" => "Projeto não encontrado.",
            "trafficinvestments.invalid_month" => "Mês inválido.",
            "trafficinvestments.invalid_year" => "Ano inválido.",
            "trafficinvestments.invalid_monthly_target" => "A meta mensal não pode ser negativa.",
            "trafficinvestments.duplicate_week" => "Semanas duplicadas no payload.",
            "traffic.investment.negative_spend" => "Valores de gasto não podem ser negativos.",
            "traffic.investment.negative_deposit" => "Valores de depósito não podem ser negativos.",
            "trafficinvestments.content_required" =>
                "Informe uma meta positiva ou ao menos um depósito/gasto maior que zero.",
            _ => string.IsNullOrWhiteSpace(fallback) ? "Dados inválidos." : fallback
        };

    private static string TranslateDepositValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "trafficdeposits.project_not_found" => "Projeto não encontrado.",
            "trafficdeposits.invalid_amount" => "O valor do aporte não pode ser negativo.",
            "trafficdeposits.amount_required" => "O valor do aporte deve ser maior que zero.",
            "trafficdeposits.notes_too_long" => "As observações excedem o limite permitido.",
            _ => string.IsNullOrWhiteSpace(fallback) ? "Dados inválidos." : fallback
        };
}
