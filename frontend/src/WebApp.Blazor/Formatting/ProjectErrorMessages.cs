using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class ProjectErrorMessages
{
    public static string ForMutation(ProjectMutationResult result) =>
        result.Status switch
        {
            ProjectApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            ProjectApiStatus.Conflict => "Já existe um projeto com este nome.",
            ProjectApiStatus.NotFound => "Projeto não encontrado.",
            ProjectApiStatus.Forbidden => "Acesso negado.",
            ProjectApiStatus.Error => "Não foi possível salvar o projeto.",
            _ => "Não foi possível salvar o projeto."
        };

    public static string ForList(ProjectListResult result) =>
        result.Status switch
        {
            ProjectApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os projetos."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "projects.name_required" => "O nome é obrigatório.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
