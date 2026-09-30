using Core.Domain.PayrollCalculation;

namespace Core.Domain;

/// <summary>
/// Snapshot persistível de cargo + entradas de projeto para mudança de cargo (REGRAS §5.2).
/// Usa FKs em vez de entidades EF para serialização JSON.
/// </summary>
public sealed class PayrollRoleChangeSnapshot
{
    public Guid? DepartmentId { get; set; }

    public Guid? CareerLevelId { get; set; }

    public decimal? FullBaseSalary { get; set; }

    public GoalTier GoalTier { get; set; }

    public decimal? FinalSalary { get; set; }

    public Guid? TrafficSeniorLevelId { get; set; }

    public IReadOnlyList<ManagementRevenueEntryInput> ManagementRevenueEntries { get; set; } = [];

    public IReadOnlyList<TrafficProjectEntryInput> TrafficProjectEntries { get; set; } = [];

    public decimal SupervisorAnalystRevenue { get; set; }

    public IReadOnlyList<SupervisorProjectEntryInput> SupervisorProjectEntries { get; set; } = [];

    public IReadOnlyList<CommercialAnalystProjectEntryInput> CommercialProjectEntries { get; set; } = [];

    public int BetanoInternaCount { get; set; }

    public int BetanoMundoBetCount { get; set; }

    public IReadOnlyList<ProjectEntryInput> ProjectEntries { get; set; } = [];

    public IReadOnlyList<RateioProjectEntryInput> RateioProjectEntries { get; set; } = [];

    public IReadOnlyList<ProjectCalculationSnapshot> ProjectSnapshots { get; set; } = [];

    public Guid? CommissionPayingProjectId { get; set; }

    public IReadOnlyList<ComplementPayingProjectInput> ComplementPayingProjects { get; set; } = [];
}
