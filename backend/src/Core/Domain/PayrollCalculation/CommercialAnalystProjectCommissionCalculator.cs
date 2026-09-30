using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão automática do analista comercial por projeto (REGRAS §6.1).
/// </summary>
public static class CommercialAnalystProjectCommissionCalculator
{
    public sealed record ProjectBreakdown(
        decimal Commission,
        int IgamingFtdCount,
        decimal PerProjectFtdBonus);

    public static decimal Calculate(
        IReadOnlyList<CommercialAnalystProjectEntryInput> projectEntries,
        CareerLevel? careerLevel) =>
        CalculateBreakdowns(projectEntries, careerLevel).Sum(b => b.Commission);

    public static IReadOnlyList<ProjectBreakdown> CalculateBreakdowns(
        IReadOnlyList<CommercialAnalystProjectEntryInput> projectEntries,
        CareerLevel? careerLevel)
    {
        if (projectEntries.Count == 0 || careerLevel is null)
        {
            return [];
        }

        return projectEntries
            .Select(entry => CalculateProject(entry, careerLevel))
            .ToList();
    }

    public static ProjectBreakdown CalculateProject(
        CommercialAnalystProjectEntryInput entry,
        CareerLevel careerLevel)
    {
        var igamingFtd = Math.Max(0, entry.FtdTotal - entry.FtdSuperbet);

        var ftdRates = CommercialAnalystRateSelector.SelectFtd(
            careerLevel,
            entry.IsFtdGoalReached,
            entry.IsProjectFtdGoalReached);
        var salesRates = CommercialAnalystRateSelector.SelectSales(
            careerLevel,
            entry.IsSalesGoalReached,
            entry.IsProjectSalesGoalReached);

        var ftdIgamingCommission = Money.FromDecimal(igamingFtd * ftdRates.FtdRate)
            .RoundToCurrencyScale()
            .Amount;
        var ftdSuperbetCommission = Money.FromDecimal(entry.FtdSuperbet * careerLevel.FtdSuperbetRate)
            .RoundToCurrencyScale()
            .Amount;
        var perProjectFtdBonus = CalculateTierBonus(
            igamingFtd,
            careerLevel.FtdBonusEvery,
            careerLevel.FtdBonusValue);
        var cpaCommission = Money.FromDecimal(entry.CpaCount * careerLevel.DefaultCpaValue)
            .RoundToCurrencyScale()
            .Amount;
        var salesCommission = Percentage.FromPercentPoints(salesRates.SalesPct)
            .ApplyTo(Money.FromDecimal(entry.SalesAmount))
            .RoundToCurrencyScale()
            .Amount;
        var salesBonus = CalculateTierBonusDecimal(
            entry.SalesAmount,
            careerLevel.SalesBonusEvery,
            careerLevel.SalesBonusValue);
        var revCommission = Percentage.FromPercentPoints(careerLevel.RevPct)
            .ApplyTo(Money.FromDecimal(entry.Rev))
            .RoundToCurrencyScale()
            .Amount;

        var commission = ftdIgamingCommission
            + ftdSuperbetCommission
            + perProjectFtdBonus
            + cpaCommission
            + salesCommission
            + salesBonus
            + revCommission;

        return new ProjectBreakdown(commission, igamingFtd, perProjectFtdBonus);
    }

    private static decimal CalculateTierBonus(int count, int every, decimal value)
    {
        if (every <= 0 || value <= 0m || count <= 0)
        {
            return 0m;
        }

        return Money.FromDecimal(Math.Floor(count / (decimal)every) * value)
            .RoundToCurrencyScale()
            .Amount;
    }

    private static decimal CalculateTierBonusDecimal(decimal amount, decimal every, decimal value)
    {
        if (every <= 0m || value <= 0m || amount <= 0m)
        {
            return 0m;
        }

        return Money.FromDecimal(Math.Floor(amount / every) * value)
            .RoundToCurrencyScale()
            .Amount;
    }
}
