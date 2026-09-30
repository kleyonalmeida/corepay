namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Consolida alocações de múltiplos períodos de mudança de cargo (REGRAS §5.2).
/// </summary>
public static class ProjectTotalsMerger
{
    public static IReadOnlyList<ProjectTotalAllocation> Merge(
        IEnumerable<IReadOnlyList<ProjectTotalAllocation>> periodTotals)
    {
        var accumulator = new ProjectTotalsAccumulator();

        foreach (var period in periodTotals)
        {
            accumulator.AddRange(period);
        }

        return accumulator.ToList();
    }

    public static void ApplyManualBonuses(
        ProjectTotalsAccumulator accumulator,
        IReadOnlyList<BonusEntryInput> bonusEntries)
    {
        foreach (var bonus in bonusEntries)
        {
            if (bonus.ProjectId.HasValue)
            {
                accumulator.AddManualBonus(bonus.ProjectId.Value, bonus.Value);
            }
        }
    }
}
