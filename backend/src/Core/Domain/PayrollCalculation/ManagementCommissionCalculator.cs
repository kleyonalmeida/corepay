using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão da Gerência por registro de faturamento líquido (REGRAS §6.4).
/// </summary>
public static class ManagementCommissionCalculator
{
    public static decimal Calculate(
        IReadOnlyList<ManagementRevenueEntryInput> revenueEntries,
        CareerLevel? careerLevel,
        GoalTier goalTier)
    {
        if (revenueEntries.Count == 0 || careerLevel is null)
        {
            return 0m;
        }

        var pct = ManagementCommissionPercentSelector.Select(careerLevel, goalTier);
        var factor = careerLevel.NetRevenueFactor;

        if (factor == 0m || pct == 0m)
        {
            return 0m;
        }

        var total = 0m;
        foreach (var entry in revenueEntries)
        {
            var adjustedBase = Money.FromDecimal(entry.NetRevenue * (factor / 100m))
                .RoundToCurrencyScale()
                .Amount;
            var commission = Percentage.FromPercentPoints(pct)
                .ApplyTo(Money.FromDecimal(adjustedBase))
                .RoundToCurrencyScale()
                .Amount;

            total += commission;
        }

        return total;
    }
}
