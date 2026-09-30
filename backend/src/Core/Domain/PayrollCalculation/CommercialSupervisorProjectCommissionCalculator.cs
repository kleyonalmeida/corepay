using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão automática do supervisor comercial por projeto (REGRAS §6.2).
/// </summary>
public static class CommercialSupervisorProjectCommissionCalculator
{
    public static decimal Calculate(
        IReadOnlyList<SupervisorProjectEntryInput> projectEntries,
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
        SupervisorProjectEntryInput entry,
        CareerLevel careerLevel)
    {
        var rates = CommercialSupervisorRateSelector.Select(
            careerLevel,
            entry.IsProjectFtdGoalReached,
            entry.IsProjectSalesGoalReached);

        var ftdOther = Math.Max(0, entry.FtdTotal - entry.FtdSuperbet);

        var superbetCommission = Money.FromDecimal(entry.FtdSuperbet * rates.FtdSuperbetRate)
            .RoundToCurrencyScale()
            .Amount;
        var otherCommission = Money.FromDecimal(ftdOther * rates.FtdOtherRate)
            .RoundToCurrencyScale()
            .Amount;
        var salesCommission = Percentage.FromPercentPoints(rates.SalesPct)
            .ApplyTo(Money.FromDecimal(entry.SalesAmount))
            .RoundToCurrencyScale()
            .Amount;
        var revCommission = Percentage.FromPercentPoints(rates.RevPct)
            .ApplyTo(Money.FromDecimal(entry.AnalystRev))
            .RoundToCurrencyScale()
            .Amount;

        return superbetCommission
            + otherCommission
            + salesCommission
            + revCommission
            + entry.DeviceRecharge
            + entry.BonusCpa;
    }
}
