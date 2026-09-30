using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Bônus de meta (% do setor) sobre a base correta por perfil — REGRAS §6.9.
/// </summary>
public static class GoalBonusCalculator
{
    public static decimal Calculate(
        PayrollEntryInput input,
        CalculationProfile profile,
        decimal proportionalFixed,
        IReadOnlyList<FixedAllocationCalculator.Allocation> fixedAllocations)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.GoalTier == GoalTier.None || input.Department is null)
        {
            return 0m;
        }

        var goalBonusPercentage = input.Department.GoalBonusPercentage;
        if (goalBonusPercentage <= 0m)
        {
            return 0m;
        }

        var baseAmount = profile switch
        {
            CalculationProfile.Tipster => CalculateTipsterBase(input, proportionalFixed),
            CalculationProfile.AllocatedFixed => CalculateAllocatedFixedBase(
                proportionalFixed,
                fixedAllocations,
                input.ProjectSnapshots),
            _ => proportionalFixed
        };

        return Percentage.FromPercentPoints(goalBonusPercentage)
            .ApplyTo(Money.FromDecimal(baseAmount))
            .RoundToCurrencyScale()
            .Amount;
    }

    private static decimal CalculateTipsterBase(PayrollEntryInput input, decimal proportionalFixed)
    {
        var revenue = input.ProjectEntries.Sum(entry => entry.Value);
        return revenue > 0m ? revenue : proportionalFixed;
    }

    private static decimal CalculateAllocatedFixedBase(
        decimal proportionalFixed,
        IReadOnlyList<FixedAllocationCalculator.Allocation> fixedAllocations,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots)
    {
        var feiraAllocation = fixedAllocations
            .Where(allocation => FixedAllocationCalculator.ExcludesGoalBonus(
                allocation.ProjectId,
                projectSnapshots))
            .Sum(allocation => allocation.Amount);

        return proportionalFixed - feiraAllocation;
    }
}
