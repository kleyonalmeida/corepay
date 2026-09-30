using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class CollaboratorErrorMessages
{
    public static string ForList(CollaboratorListResult result) =>
        result.Status switch
        {
            CollaboratorApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os colaboradores."
        };

    public static string ForGetById(CollaboratorGetResult result) =>
        result.Status switch
        {
            CollaboratorApiStatus.Forbidden => "Colaborador fora do seu escopo de setores.",
            CollaboratorApiStatus.NotFound => "Colaborador não encontrado.",
            _ => "Não foi possível carregar o colaborador."
        };

    public static string ForMutation(CollaboratorMutationResult result) =>
        result.Status switch
        {
            CollaboratorApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            CollaboratorApiStatus.NotFound => "Colaborador não encontrado.",
            CollaboratorApiStatus.Forbidden => "Colaborador fora do seu escopo de setores.",
            CollaboratorApiStatus.Error => "Não foi possível salvar o colaborador.",
            _ => "Não foi possível salvar o colaborador."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "collaborators.name_required" => "O nome é obrigatório.",
            "collaborators.department_required" => "O setor é obrigatório.",
            "collaborators.department_not_found" => "Setor não encontrado.",
            "collaborators.careerlevel_not_found" => "Nível de carreira não encontrado.",
            "collaborators.careerlevel_department_mismatch" => "O nível selecionado não pertence ao setor.",
            "collaborators.dismissal_date_required" => "Informe a data de demissão para inativar o colaborador.",
            "collaborators.invalid_dates" => "A data de demissão não pode ser anterior à admissão.",
            "collaborators.invalid_values" => "O salário não pode ser negativo.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
