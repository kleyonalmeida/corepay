using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class ProjectRevenueErrorMessages
{
    public static string ForMutation(ProjectRevenueMutationResult result) =>
        result.Status switch
        {
            ProjectRevenueApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            ProjectRevenueApiStatus.Conflict => "Já existe faturamento para este projeto na competência selecionada.",
            ProjectRevenueApiStatus.NotFound => "Registro de faturamento não encontrado.",
            ProjectRevenueApiStatus.Forbidden => "Acesso negado.",
            ProjectRevenueApiStatus.Error => "Não foi possível salvar o faturamento.",
            _ => "Não foi possível salvar o faturamento."
        };

    public static string ForList(ProjectRevenueListResult result) =>
        result.Status switch
        {
            ProjectRevenueApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            ProjectRevenueApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar o faturamento."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "projectrevenues.project_not_found" => "Projeto não encontrado.",
            "projectrevenues.invalid_month" => "Mês inválido.",
            "projectrevenues.invalid_year" => "Ano inválido.",
            "projectrevenues.invalid_values" => "Valores não podem ser negativos.",
            "projectrevenues.values_required" => "Informe ao menos um valor de iGaming ou Vendas maior que zero.",
            "projectrevenues.invalid_group_percentage" => "O percentual do grupo deve estar entre 0 e 100.",
            "projectrevenues.notes_too_long" => "As observações excedem o limite permitido.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
