using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Distribui bônus de meta entre projetos elegíveis (Feira excluída — REGRAS §6.9).
/// </summary>
public static class GoalBonusAllocationCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(
        decimal totalGoalBonus,
        IReadOnlyList<FixedAllocationCalculator.Allocation> fixedAllocations,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots)
    {
        if (totalGoalBonus <= 0m || fixedAllocations.Count == 0)
        {
            return [];
        }

        var eligible = fixedAllocations
            .Where(allocation => !FixedAllocationCalculator.ExcludesGoalBonus(
                allocation.ProjectId,
                projectSnapshots))
            .ToList();

        var eligibleBase = eligible.Sum(allocation => allocation.Amount);
        if (eligibleBase <= 0m)
        {
            return [];
        }

        var results = new List<ProjectTotalAllocation>(eligible.Count);
        var distributed = 0m;

        for (var index = 0; index < eligible.Count; index++)
        {
            var allocation = eligible[index];
            var amount = index == eligible.Count - 1
                ? Money.FromDecimal(totalGoalBonus - distributed).RoundToCurrencyScale().Amount
                : Money.FromDecimal(totalGoalBonus * allocation.Amount / eligibleBase)
                    .RoundToCurrencyScale()
                    .Amount;

            distributed += amount;
            results.Add(new ProjectTotalAllocation(allocation.ProjectId, amount));
        }

        return results;
    }
}
