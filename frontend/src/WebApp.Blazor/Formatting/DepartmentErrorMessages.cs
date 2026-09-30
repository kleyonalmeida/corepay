using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class DepartmentErrorMessages
{
    public static string ForMutation(DepartmentMutationResult result) =>
        result.Status switch
        {
            DepartmentApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            DepartmentApiStatus.Conflict => "Já existe um setor com este nome.",
            DepartmentApiStatus.NotFound => "Setor não encontrado.",
            DepartmentApiStatus.Forbidden => "Acesso negado.",
            DepartmentApiStatus.Error => "Não foi possível salvar o setor.",
            _ => "Não foi possível salvar o setor."
        };

    public static string ForList(DepartmentListResult result) =>
        result.Status switch
        {
            DepartmentApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os setores."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "departments.name_required" => "O nome é obrigatório.",
            "departments.invalid_values" => "Valores numéricos não podem ser negativos.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
