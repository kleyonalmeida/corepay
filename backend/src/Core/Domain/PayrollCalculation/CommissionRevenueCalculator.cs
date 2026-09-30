using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Calcula comissão percentual sobre a soma dos valores de faturamento dos projetos.
/// </summary>
public static class CommissionRevenueCalculator
{
    public static decimal Calculate(
        IReadOnlyList<ProjectEntryInput> projectEntries,
        decimal commissionPercentPoints)
    {
        var revenue = projectEntries.Sum(entry => entry.Value);
        var commission = Percentage.FromPercentPoints(commissionPercentPoints)
            .ApplyTo(Money.FromDecimal(revenue));

        return commission.RoundToCurrencyScale().Amount;
    }
}
