namespace WebApp.Blazor.Components.Ui;

internal static class StatusBadgeMetadata
{
    internal static (string Label, string CssClass) Get(StatusKind kind) => kind switch
    {
        StatusKind.Draft => ("Rascunho", "status-badge--draft"),
        StatusKind.PendingApproval => ("Aguard. Aprovação", "status-badge--pending-approval"),
        StatusKind.Approved => ("Aprovada", "status-badge--approved"),
        StatusKind.Rejected => ("Reprovada", "status-badge--rejected"),
        StatusKind.Paid => ("Paga", "status-badge--paid"),
        StatusKind.Pending => ("Pendente", "status-badge--pending"),
        StatusKind.Active => ("Ativo", "status-badge--active"),
        StatusKind.Inactive => ("Inativo", "status-badge--inactive"),
        StatusKind.NfSent => ("Enviada", "status-badge--nf-sent"),
        StatusKind.NfPending => ("Pendente", "status-badge--nf-pending"),
        _ => ("Desconhecido", "status-badge--draft")
    };
}
