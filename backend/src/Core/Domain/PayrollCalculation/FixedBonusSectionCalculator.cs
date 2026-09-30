using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Ramo §6.9: FixedBonus, Tipster e AllocatedFixed.
/// </summary>
public static class FixedBonusSectionCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input, CalculationProfile profile)
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

        var groupCommission = profile == CalculationProfile.Tipster
            ? TipsterGroupCommissionCalculator.Calculate(input)
            : 0m;

        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var total = Money.FromDecimal(
                proportionalBase + goalBonus + groupCommission + bonuses - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = proportionalBase,
            CommissionAmount = 0m,
            GoalBonusAmount = goalBonus,
            GroupCommissionAmount = groupCommission
        };
    }
}
