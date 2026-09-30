using Core.Domain.PayrollCalculation;

namespace Core.Domain;

/// <summary>
/// Blocos variáveis de entrada da linha de colaborador, persistidos como JSON.
/// </summary>
public sealed class PayrollCollaboratorEntryPayload
{
    public IReadOnlyList<ProjectEntryInput> ProjectEntries { get; set; } = [];

    public IReadOnlyList<RateioProjectEntryInput> RateioProjectEntries { get; set; } = [];

    public IReadOnlyList<CommercialAnalystProjectEntryInput> CommercialProjectEntries { get; set; } = [];

    public IReadOnlyList<SupervisorProjectEntryInput> SupervisorProjectEntries { get; set; } = [];

    public IReadOnlyList<TrafficProjectEntryInput> TrafficProjectEntries { get; set; } = [];

    public IReadOnlyList<ManagementRevenueEntryInput> ManagementRevenueEntries { get; set; } = [];

    public IReadOnlyList<BonusEntryInput> BonusEntries { get; set; } = [];

    public IReadOnlyList<DeductionEntryInput> DeductionEntries { get; set; } = [];

    public IReadOnlyList<ComplementPayingProjectInput> ComplementPayingProjects { get; set; } = [];

    public IReadOnlyList<ProjectCalculationSnapshot> ProjectSnapshots { get; set; } = [];

    public IReadOnlyList<PayrollRoleChangeEntry> RoleChanges { get; set; } = [];

    /// <summary>Resultado calculado persistido no save/submit (snapshot mínimo Fase 7.6).</summary>
    public PayrollEntryResult? CalculatedResult { get; set; }

    /// <summary>Breakdown normalizado por projeto para exibição (Fase 6.2).</summary>
    public IReadOnlyList<ProjectTotalAllocation> DisplayProjectTotals { get; set; } = [];
}
