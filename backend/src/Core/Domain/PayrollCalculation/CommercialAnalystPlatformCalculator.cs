using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Valor pago pela Lastlink/Hubla sobre vendas — sempre % base; Hubla teto 4% (REGRAS §5.4, §6.1).
/// </summary>
public static class CommercialAnalystPlatformCalculator
{
    public const decimal HublaMaxPct = 4m;

    public static decimal Calculate(
        IReadOnlyList<CommercialAnalystProjectEntryInput> projectEntries,
        CareerLevel? careerLevel)
    {
        if (projectEntries.Count == 0 || careerLevel is null)
        {
            return 0m;
        }

        var total = 0m;

        foreach (var entry in projectEntries)
        {
            total += CalculateProject(entry, careerLevel);
        }

        return total;
    }

    public static decimal CalculateProject(
        CommercialAnalystProjectEntryInput entry,
        CareerLevel careerLevel)
    {
        if (entry.SalesAmount <= 0m)
        {
            return 0m;
        }

        var pct = entry.Platform == ProjectPlatform.Hubla
            ? Math.Min(careerLevel.SalesPctBase, HublaMaxPct)
            : careerLevel.SalesPctBase;

        return Percentage.FromPercentPoints(pct)
            .ApplyTo(Money.FromDecimal(entry.SalesAmount))
            .RoundToCurrencyScale()
            .Amount;
    }
}
