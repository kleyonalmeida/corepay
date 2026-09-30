using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class UserErrorMessages
{
    public static string ForMutation(UserMutationResult result) =>
        result.Status switch
        {
            UserApiStatus.ValidationError => TranslateValidation(result.ErrorCode, result.Message),
            UserApiStatus.Conflict => "Já existe um usuário com este e-mail.",
            UserApiStatus.NotFound => "Usuário não encontrado.",
            UserApiStatus.Forbidden => TranslateForbidden(result.ErrorCode),
            UserApiStatus.Error => "Não foi possível salvar o usuário.",
            _ => "Não foi possível salvar o usuário."
        };

    public static string ForList(UserListResult result) =>
        result.Status switch
        {
            UserApiStatus.Forbidden => "Acesso negado.",
            _ => "Não foi possível carregar os usuários."
        };

    public static string ForDelete(UserDeleteResult result) =>
        result.Status switch
        {
            UserApiStatus.Forbidden => TranslateForbidden(result.ErrorCode),
            UserApiStatus.NotFound => "Usuário não encontrado.",
            UserApiStatus.Error => "Não foi possível excluir o usuário.",
            _ => "Não foi possível excluir o usuário."
        };

    private static string TranslateForbidden(string? errorCode) =>
        errorCode switch
        {
            "users.superadmin_role_protected" =>
                "A role SuperAdmin não pode ser removida deste usuário.",
            "users.superadmin_protected" =>
                "Usuários SuperAdmin não podem ser excluídos.",
            "users.superadmin_assignment_forbidden" =>
                "Somente SuperAdmin pode atribuir a role SuperAdmin.",
            "users.cannot_delete_self" =>
                "Você não pode excluir sua própria conta.",
            _ => "Acesso negado."
        };

    private static string TranslateValidation(string? errorCode, string? fallback) =>
        errorCode switch
        {
            "users.email_required" => "O e-mail é obrigatório.",
            "users.email_invalid" => "E-mail inválido.",
            "users.display_name_required" => "O nome é obrigatório.",
            "users.password_required" => "A senha é obrigatória.",
            "users.roles_required" => "Selecione ao menos um papel.",
            "users.role_not_found" => "Papel inválido selecionado.",
            "users.department_not_found" => "Setor inválido selecionado.",
            "users.manager_departments_required" => "Gerentes devem ter ao menos um setor selecionado.",
            _ => string.IsNullOrWhiteSpace(fallback)
                ? "Dados inválidos."
                : fallback
        };
}
