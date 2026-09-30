using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class CareerLevelErrorMessages
{
    public static string ForMutation(CareerLevelMutationResult result) =>
        result.Status switch
        {
            CareerLevelApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            CareerLevelApiStatus.Conflict => "Já existe um nível com este nome neste setor.",
            CareerLevelApiStatus.NotFound => "Nível de carreira não encontrado.",
            CareerLevelApiStatus.Forbidden => "Acesso negado.",
            CareerLevelApiStatus.Error => "Não foi possível salvar o nível.",
            _ => "Não foi possível salvar o nível."
        };

    public static string ForList(CareerLevelListResult result) =>
        result.Status switch
        {
            CareerLevelApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os níveis de carreira."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "careerlevels.name_required" => "O nome é obrigatório.",
            "careerlevels.invalid_values" => "Valores numéricos inválidos ou negativos.",
            "careerlevels.department_not_found" => "Setor selecionado não encontrado.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
