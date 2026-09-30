using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class AnalystMetricErrorMessages
{
    public static string ForList(AnalystMetricListResult result) =>
        result.Status switch
        {
            AnalystMetricApiStatus.Forbidden => "Você não tem acesso a estas métricas.",
            AnalystMetricApiStatus.ValidationError => "Os filtros informados são inválidos.",
            _ => "Não foi possível carregar as métricas de analista."
        };

    public static string ForMutation(AnalystMetricMutationResult result) =>
        result.ErrorCode switch
        {
            "analystmetrics.duplicate" => "Já existe uma métrica para este colaborador, projeto e competência.",
            "analystmetrics.invalid_counts" => "FTD e CPA devem ser valores inteiros não negativos.",
            "analystmetrics.collaborator_not_found" => "Colaborador não encontrado.",
            "analystmetrics.project_not_found" => "Projeto não encontrado.",
            "analystmetrics.department_forbidden" => "O colaborador está fora dos seus setores.",
            _ when result.Status == AnalystMetricApiStatus.Forbidden => "Você não pode alterar esta métrica.",
            _ => "Não foi possível salvar a métrica."
        };
}
