namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Snapshot de cargo + entradas de projeto para um intervalo de mudança de cargo (REGRAS §5.2).
/// </summary>
public sealed record PayrollRoleSnapshot
{
    public Department? Department { get; init; }

    public CareerLevel? CareerLevel { get; init; }

    public decimal? FullBaseSalary { get; init; }

    public GoalTier GoalTier { get; init; }

    public decimal? FinalSalary { get; init; }

    public CareerLevel? TrafficSeniorLevel { get; init; }

    public IReadOnlyList<ManagementRevenueEntryInput> ManagementRevenueEntries { get; init; } = [];

    public IReadOnlyList<TrafficProjectEntryInput> TrafficProjectEntries { get; init; } = [];

    public decimal SupervisorAnalystRevenue { get; init; }

    public IReadOnlyList<SupervisorProjectEntryInput> SupervisorProjectEntries { get; init; } = [];

    public IReadOnlyList<CommercialAnalystProjectEntryInput> CommercialProjectEntries { get; init; } = [];

    public int BetanoInternaCount { get; init; }

    public int BetanoMundoBetCount { get; init; }

    public IReadOnlyList<ProjectEntryInput> ProjectEntries { get; init; } = [];

    public IReadOnlyList<RateioProjectEntryInput> RateioProjectEntries { get; init; } = [];

    public IReadOnlyList<ProjectCalculationSnapshot> ProjectSnapshots { get; init; } = [];

    public Guid? CommissionPayingProjectId { get; init; }

    public IReadOnlyList<ComplementPayingProjectInput> ComplementPayingProjects { get; init; } = [];

    public static PayrollRoleSnapshot FromMainInput(PayrollEntryInput input) =>
        new()
        {
            Department = input.Department,
            CareerLevel = input.CareerLevel,
            FullBaseSalary = input.FullBaseSalary,
            GoalTier = input.GoalTier,
            FinalSalary = input.FinalSalary,
            TrafficSeniorLevel = input.TrafficSeniorLevel,
            ManagementRevenueEntries = input.ManagementRevenueEntries,
            TrafficProjectEntries = input.TrafficProjectEntries,
            SupervisorAnalystRevenue = input.SupervisorAnalystRevenue,
            SupervisorProjectEntries = input.SupervisorProjectEntries,
            CommercialProjectEntries = input.CommercialProjectEntries,
            BetanoInternaCount = input.BetanoInternaCount,
            BetanoMundoBetCount = input.BetanoMundoBetCount,
            ProjectEntries = input.ProjectEntries,
            RateioProjectEntries = input.RateioProjectEntries,
            ProjectSnapshots = input.ProjectSnapshots,
            CommissionPayingProjectId = input.CommissionPayingProjectId,
            ComplementPayingProjects = input.ComplementPayingProjects
        };
}
