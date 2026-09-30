using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class ReportsErrorMessages
{
    public static string ForReport(ReportsResult result) =>
        result.Status switch
        {
            ReportsApiStatus.Forbidden => "Você não tem permissão para visualizar relatórios de folha.",
            ReportsApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível carregar o relatório de folha."
        };

    public static string ForExport(ReportsExportResult result) =>
        result.Status switch
        {
            ReportsApiStatus.Forbidden => "Você não tem permissão para exportar relatórios de folha.",
            ReportsApiStatus.ValidationError => MapValidation(result.ErrorCode, result.Message),
            _ => "Não foi possível exportar o relatório de folha."
        };

    private static string MapValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "reports.invalid_year" => "Selecione um ano válido.",
            "reports.department_forbidden" => "Setor fora do seu escopo de acesso.",
            _ => fallback ?? "Verifique os filtros informados."
        };
}
