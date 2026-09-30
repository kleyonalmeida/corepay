using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto dos perfis FixedBonus, Tipster e AllocatedFixed (REGRAS §6.9).
/// </summary>
public static class FixedBonusSectionProjectTotalsCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(
        PayrollEntryInput input,
        CalculationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Department);

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var fixedAllocations = FixedAllocationCalculator.Calculate(
            proportionalBase,
            input.Department,
            input.RateioProjectEntries,
            input.ProjectEntries,
            input.ProjectSnapshots);

        var goalBonus = GoalBonusCalculator.Calculate(
            input,
            profile,
            proportionalBase,
            fixedAllocations);

        var accumulator = new ProjectTotalsAccumulator();
        accumulator.AddRangeFixed(fixedAllocations);
        accumulator.AddRange(GoalBonusAllocationCalculator.Calculate(
            goalBonus,
            fixedAllocations,
            input.ProjectSnapshots));

        if (profile == CalculationProfile.Tipster)
        {
            accumulator.AddRange(TipsterGroupCommissionAllocationCalculator.Calculate(input));
        }

        return accumulator.ToList();
    }
}
