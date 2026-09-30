using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Perfil comissão pura (REGRAS §6.6).
/// </summary>
public static class CommissionOnlyCalculator
{
    public static PayrollEntryResult Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var pct = CommissionPercentSelector.Select(input.CareerLevel, input.GoalTier);
        var commission = CommissionRevenueCalculator.Calculate(input.ProjectEntries, pct);
        var bonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var deductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        var total = Money.FromDecimal(commission + bonuses - deductions).RoundToCurrencyScale().Amount;

        return new PayrollEntryResult
        {
            TotalAmount = total,
            BaseSalary = 0m,
            CommissionAmount = commission
        };
    }
}
