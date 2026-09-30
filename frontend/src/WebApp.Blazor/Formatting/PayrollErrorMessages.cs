using WebApp.Blazor.Services;

namespace WebApp.Blazor.Formatting;

public static class PayrollErrorMessages
{
    public static string ForList(PayrollListResult result) =>
        result.Status switch
        {
            PayrollApiStatus.Forbidden => "Você não tem permissão para listar folhas.",
            _ => Translate(result.ErrorCode, result.Message ?? "Não foi possível carregar as folhas.")
        };

    public static string ForDuplicate(PayrollDuplicateResult result) =>
        result.Status switch
        {
            PayrollApiStatus.Conflict when result.ErrorCode == "payrolls.competence_duplicate" =>
                "Já existe folha para este setor na competência seguinte.",
            PayrollApiStatus.Conflict => Translate(result.ErrorCode, "Já existe uma folha para a competência de destino."),
            PayrollApiStatus.Forbidden => "Você não tem permissão para duplicar esta folha.",
            PayrollApiStatus.NotFound => "Folha não encontrada.",
            _ => Translate(result.ErrorCode, result.Message ?? "Não foi possível duplicar a folha.")
        };

    public static string ForMutation(PayrollMutationResult result) =>
        result.Status switch
        {
            PayrollApiStatus.Forbidden => "Você não tem permissão para executar esta ação.",
            PayrollApiStatus.NotFound => "Folha não encontrada.",
            PayrollApiStatus.Conflict => Translate(result.ErrorCode, result.Message ?? "Operação inválida para o status atual."),
            PayrollApiStatus.ValidationError => Translate(result.ErrorCode, result.Message ?? "Dados inválidos."),
            _ => Translate(result.ErrorCode, result.Message ?? "Não foi possível concluir a operação.")
        };

    public static string ForDelete(PayrollDeleteResult result) =>
        result.Status switch
        {
            PayrollApiStatus.Forbidden => "Você não tem permissão para excluir esta folha.",
            PayrollApiStatus.NotFound => "Folha não encontrada.",
            _ => Translate(result.ErrorCode, result.Message ?? "Não foi possível excluir a folha.")
        };

    public static string Translate(string? errorCode, string? fallback = null) =>
        errorCode switch
        {
            "payrolls.competence_duplicate" => "Já existe uma folha para este setor e competência.",
            "payrolls.department_forbidden" => "Você não tem permissão para este setor.",
            "payrolls.status_not_editable" => "Esta folha não pode ser editada no status atual.",
            "payrolls.status_not_approvable" => "Esta folha não pode ser aprovada no status atual.",
            "payrolls.status_not_rejectable" => "Esta folha não pode ser reprovada no status atual.",
            "payrolls.rejection_comment_required" => "Informe um comentário de reprovação.",
            "payrolls.pending_approval_cannot_pay" => "Folhas de pagamento aguardando aprovação não podem ser pagas.",
            "payrolls.no_entries" => "A folha não possui colaboradores para marcar como pago.",
            "payrolls.entries_required" => "Adicione ao menos um colaborador.",
            "payrolls.collaborator_not_in_department" => "O colaborador não pertence ao setor da folha.",
            "payrolls.collaborator_inactive" => "Colaboradores inativos não podem ser adicionados.",
            "payrolls.collaborator_already_in_payroll" => "Este colaborador já está na folha.",
            "payrolls.collaborator_not_found" => "Colaborador não encontrado.",
            "payrolls.invalid_month" => "Mês inválido.",
            "payrolls.invalid_year" => "Ano inválido.",
            "payrolls.entry_not_found" => "Linha da folha não encontrada.",
            "payrolls.project_not_found" => "Projeto não encontrado.",
            "payrolls.career_level_not_found" => "Nível de carreira não encontrado.",
            "payroll.complement_paying_projects_invalid_sum" =>
                "A soma dos percentuais de complemento deve ser exatamente 100%.",
            "traffic.manual_rateio_not_allowed" =>
                "Rateio manual não é permitido para tráfego pago.",
            "payrolls.approved_entry_readonly" =>
                "Colaboradores já aprovados não podem ser alterados.",
            "payrolls.entry_duplicate" =>
                "Entradas duplicadas no envio da folha.",
            _ => fallback ?? "Não foi possível concluir a operação."
        };
}
