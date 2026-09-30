using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Comissão do Líder de Projetos por projeto: líquido 80%, acréscimo de baixo faturamento e teto 2,1% (REGRAS §6.5).
/// </summary>
public static class ProjectLeaderCommissionCalculator
{
    public const decimal NetRevenueFactor = 0.80m;

    public const decimal MaxCommissionPercentPoints = 2.1m;

    public static decimal Calculate(
        IReadOnlyList<ProjectEntryInput> projectEntries,
        CareerLevel? careerLevel,
        Department? department,
        GoalTier goalTier)
    {
        if (projectEntries.Count == 0 || careerLevel is null)
        {
            return 0m;
        }

        var basePct = ProjectLeaderCommissionPercentSelector.Select(careerLevel, goalTier);
        var threshold = department?.LowRevenueThreshold ?? 0m;
        var lowRevenueBonusPct = department?.LowRevenueBonusPct ?? 0m;

        var total = 0m;
        foreach (var entry in projectEntries)
        {
            total += CalculateProject(entry, careerLevel, department, goalTier);
        }

        return total;
    }

    public static decimal CalculateProject(
        ProjectEntryInput entry,
        CareerLevel? careerLevel,
        Department? department,
        GoalTier goalTier)
    {
        if (careerLevel is null)
        {
            return 0m;
        }

        var basePct = ProjectLeaderCommissionPercentSelector.Select(careerLevel, goalTier);
        var threshold = department?.LowRevenueThreshold ?? 0m;
        var lowRevenueBonusPct = department?.LowRevenueBonusPct ?? 0m;
        var lowBonus = entry.Value < threshold ? lowRevenueBonusPct : 0m;
        var finalPct = Math.Min(basePct + lowBonus, MaxCommissionPercentPoints);
        var netRevenue = Money.FromDecimal(entry.Value * NetRevenueFactor).RoundToCurrencyScale().Amount;

        return Percentage.FromPercentPoints(finalPct)
            .ApplyTo(Money.FromDecimal(netRevenue))
            .RoundToCurrencyScale()
            .Amount;
    }
}
