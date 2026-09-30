using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil fixo + comissão (REGRAS §6.7).
/// </summary>
public static class FixedCommissionCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var pct = CommissionPercentSelector.Select(input.CareerLevel, input.GoalTier);
        var commission = CommissionRevenueCalculator.Calculate(input.ProjectEntries, pct);
        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var total = Money.FromDecimal(proportionalBase + commission + bonuses - deductions)
            .RoundToCurrencyScale()
            .Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = proportionalBase,
            CommissionAmount = commission
        };
    }
}
