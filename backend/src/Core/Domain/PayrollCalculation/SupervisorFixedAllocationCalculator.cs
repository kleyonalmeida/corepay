using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Rateio do fixo do supervisor comercial — divisão igualitária entre projetos elegíveis (REGRAS §6.2).
/// Projetos com <see cref="ProjectCalculationSnapshot.ExcludesSupervisorFixedAllocation"/> ficam de fora.
/// </summary>
public static class SupervisorFixedAllocationCalculator
{
    public static IReadOnlyList<FixedAllocationCalculator.Allocation> Calculate(
        decimal proportionalFixed,
        IReadOnlyList<SupervisorProjectEntryInput> supervisorProjectEntries,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots)
    {
        if (proportionalFixed <= 0m)
        {
            return [];
        }

        var eligible = supervisorProjectEntries
            .Where(entry => !ExcludesSupervisorFixedAllocation(entry.ProjectId, projectSnapshots))
            .ToList();

        if (eligible.Count == 0)
        {
            return [];
        }

        var equalShare = Money.FromDecimal(proportionalFixed / eligible.Count)
            .RoundToCurrencyScale()
            .Amount;
        var distributed = 0m;
        var results = new List<FixedAllocationCalculator.Allocation>(eligible.Count);

        for (var index = 0; index < eligible.Count; index++)
        {
            var amount = index == eligible.Count - 1
                ? Money.FromDecimal(proportionalFixed - distributed).RoundToCurrencyScale().Amount
                : equalShare;

            distributed += amount;
            results.Add(new FixedAllocationCalculator.Allocation(eligible[index].ProjectId, amount));
        }

        return results;
    }

    public static bool ExcludesSupervisorFixedAllocation(
        Guid projectId,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots) =>
        projectSnapshots.FirstOrDefault(snapshot => snapshot.ProjectId == projectId)
            ?.ExcludesSupervisorFixedAllocation ?? false;
}
