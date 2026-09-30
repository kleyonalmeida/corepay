using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Consolida resultados de múltiplos períodos de mudança de cargo (REGRAS §5.2).
/// </summary>
public static class PayrollEntryResultMerger
{
    public static PayrollEntryResult Merge(
        IReadOnlyList<PayrollEntryResult> periods,
        decimal manualBonuses,
        decimal manualDeductions)
    {
        if (periods.Count == 0)
        {
            return PayrollEntryResult.Zero;
        }

        var merged = new PayrollEntryResult
        {
            TotalAmount = periods.Sum(period => period.TotalAmount),
            BaseSalary = periods.Sum(period => period.BaseSalary),
            CommissionAmount = periods.Sum(period => period.CommissionAmount),
            GoalBonusAmount = periods.Sum(period => period.GoalBonusAmount),
            GroupCommissionAmount = periods.Sum(period => period.GroupCommissionAmount),
            PlatformTotal = periods.Sum(period => period.PlatformTotal)
        };

        var total = Money.FromDecimal(merged.TotalAmount + manualBonuses - manualDeductions)
            .RoundToCurrencyScale()
            .Amount;

        return merged with { TotalAmount = total };
    }
}
