using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil fixo + comissão + bônus de meta — Affiliates (REGRAS §6.8).
/// </summary>
public static class FixedCommissionBonusCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        if (input.FinalSalary is > 0)
        {
            var total = Money.FromDecimal(input.FinalSalary.Value + bonuses - deductions)
                .RoundToCurrencyScale()
                .Amount;

            return new PayrollEntryResult
            {
                TotalAmount = total,
                BaseSalary = input.FinalSalary.Value,
                CommissionAmount = 0m
            };
        }

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var pct = CommissionPercentSelector.Select(input.CareerLevel, input.GoalTier);
        var commission = CommissionRevenueCalculator.Calculate(input.ProjectEntries, pct);
        var goalBonus = input.GoalTier != GoalTier.None
            ? input.CareerLevel?.GoalBonusValue ?? 0m
            : 0m;

        var totalNormal = Money.FromDecimal(
                proportionalBase + commission + goalBonus + bonuses - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = totalNormal,
            BaseSalary = proportionalBase,
            CommissionAmount = commission
        };
    }
}
