using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão de grupo Tipster: soma(%) × R$/1% + floor(%/20) × R$/20% — REGRAS §6.9.
/// </summary>
public static class TipsterGroupCommissionCalculator
{
    public static decimal Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var totalGroupPercentage = input.ProjectEntries.Sum(entry => entry.GroupPercentage);
        if (totalGroupPercentage <= 0m)
        {
            return 0m;
        }

        var perPercent = input.CareerLevel?.GroupCommissionPerPercent ?? 0m;
        var perTwentyPercent = input.CareerLevel?.GroupCommissionPer20Percent ?? 0m;

        var perPercentAmount = totalGroupPercentage * perPercent;
        var vipBands = decimal.Floor(totalGroupPercentage / 20m);
        var vipAmount = vipBands * perTwentyPercent;

        return Money.FromDecimal(perPercentAmount + vipAmount).RoundToCurrencyScale().Amount;
    }
}
