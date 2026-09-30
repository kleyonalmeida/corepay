using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Bônus FTD combinado sobre a soma dos projetos — paga só o extra vs bônus por projeto (REGRAS §6.1).
/// </summary>
public static class CommercialAnalystCombinedFtdBonusCalculator
{
    public static decimal CalculateExtra(
        IReadOnlyList<CommercialAnalystProjectCommissionCalculator.ProjectBreakdown> breakdowns,
        CareerLevel? careerLevel)
    {
        if (breakdowns.Count == 0 || careerLevel is null)
        {
            return 0m;
        }

        if (careerLevel.FtdBonusEvery <= 0 || careerLevel.FtdBonusValue <= 0m)
        {
            return 0m;
        }

        var totalIgamingFtd = breakdowns.Sum(b => b.IgamingFtdCount);
        var combinedBonus = Money.FromDecimal(
                Math.Floor(totalIgamingFtd / (decimal)careerLevel.FtdBonusEvery)
                * careerLevel.FtdBonusValue)
            .RoundToCurrencyScale()
            .Amount;
        var perProjectBonusSum = breakdowns.Sum(b => b.PerProjectFtdBonus);

        return Math.Max(0m, combinedBonus - perProjectBonusSum);
    }
}
