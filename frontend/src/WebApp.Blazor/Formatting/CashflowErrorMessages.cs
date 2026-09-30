using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class CashflowErrorMessages
{
    public static string ForList(CashflowListResult result) =>
        result.Status switch
        {
            CashflowApiStatus.Forbidden => "Você não tem permissão para visualizar o fluxo de caixa.",
            CashflowApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível carregar os lançamentos."
        };

    public static string ForMutation(CashflowMutationResult result) =>
        result.Status switch
        {
            CashflowApiStatus.Forbidden => "Você não tem permissão para alterar lançamentos.",
            CashflowApiStatus.NotFound => "Lançamento não encontrado.",
            CashflowApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível salvar o lançamento."
        };

    public static string ForCreate(CashflowCreateResult result) =>
        result.Status switch
        {
            CashflowApiStatus.Forbidden => "Você não tem permissão para criar lançamentos.",
            CashflowApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível criar o lançamento."
        };

    public static string ForDelete(CashflowDeleteResult result) =>
        result.Status switch
        {
            CashflowApiStatus.Forbidden => "Você não tem permissão para excluir lançamentos.",
            CashflowApiStatus.NotFound => "Lançamento não encontrado.",
            _ => "Não foi possível excluir o lançamento."
        };

    public static string ForReport(CashflowReportResult result) =>
        result.Status switch
        {
            CashflowApiStatus.Forbidden => "Você não tem permissão para visualizar o relatório de caixa.",
            CashflowApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível carregar o relatório de caixa."
        };

    private static string MapValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "cashflow.invalid_amount" => "Informe um valor maior que zero.",
            "cashflow.category_type_mismatch" => "A categoria não é válida para o tipo selecionado.",
            "cashflow.payment_method_required" => "Selecione uma forma de pagamento.",
            "cashflow.department_required" => "Selecione um setor.",
            "cashflow.entry_exit_fields_forbidden" => "Campos de saída não são permitidos em entradas.",
            "cashflow.installments_saida_only" => "Parcelamento só é permitido em saídas.",
            _ => fallback ?? "Verifique os dados informados."
        };
}
