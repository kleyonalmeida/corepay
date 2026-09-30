namespace Core.Application.Payrolls;

/// <summary>
/// Capacidades de workflow expostas no detalhe da folha (Fase 8.1).
/// Reflete permissão JWT + status atual; mutações reais na Fase 8.2.
/// </summary>
public sealed record PayrollAllowedActionsResponse(
    bool ApprovePayroll,
    bool RejectPayroll,
    bool ApproveEntry,
    bool Edit,
    bool PayPayroll,
    bool PayEntry,
    bool ToggleNf,
    bool PostApprovalAdjustments,
    bool Delete,
    bool Recalculate,
    bool AddCollaborator);
