using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Distribui o fixo proporcional entre projetos (rateio igualitário, manual ou Lima Karttos — §6.9).
/// </summary>
public static class FixedAllocationCalculator
{
    public sealed record Allocation(Guid ProjectId, decimal Amount);

    public static IReadOnlyList<Allocation> Calculate(
        decimal proportionalFixed,
        Department department,
        IReadOnlyList<RateioProjectEntryInput> rateioProjectEntries,
        IReadOnlyList<ProjectEntryInput> projectEntries,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots)
    {
        ArgumentNullException.ThrowIfNull(department);

        if (proportionalFixed <= 0m)
        {
            return [];
        }

        if (DepartmentCalculationFlags.RoutesFixedToLimaKarttos(department))
        {
            var lima = projectSnapshots.FirstOrDefault(snapshot => snapshot.IsDefaultAllocationTarget)
                ?? throw new InvalidOperationException(
                    "Projeto Lima Karttos (IsDefaultAllocationTarget) é obrigatório para setores com RoutesFixedToLimaKarttos.");

            return [new Allocation(lima.ProjectId, proportionalFixed)];
        }

        var entries = ResolveRateioEntries(rateioProjectEntries, projectEntries);
        if (entries.Count == 0)
        {
            return [];
        }

        return DistributeFixed(proportionalFixed, entries);
    }

    private static IReadOnlyList<(Guid ProjectId, decimal? ManualValue)> ResolveRateioEntries(
        IReadOnlyList<RateioProjectEntryInput> rateioProjectEntries,
        IReadOnlyList<ProjectEntryInput> projectEntries)
    {
        if (rateioProjectEntries.Count > 0)
        {
            return rateioProjectEntries
                .Select(entry => (entry.ProjectId, entry.RateioValue))
                .ToList();
        }

        return projectEntries
            .Select(entry => (entry.ProjectId, (decimal?)null))
            .ToList();
    }

    private static IReadOnlyList<Allocation> DistributeFixed(
        decimal total,
        IReadOnlyList<(Guid ProjectId, decimal? ManualValue)> entries)
    {
        var manualSum = entries
            .Where(entry => entry.ManualValue.HasValue)
            .Sum(entry => entry.ManualValue!.Value);

        if (manualSum > total)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entries),
                manualSum,
                "A soma dos rateio_value manuais não pode exceder o fixo proporcional.");
        }

        var manualEntries = entries.Where(entry => entry.ManualValue.HasValue).ToList();
        var autoEntries = entries.Where(entry => !entry.ManualValue.HasValue).ToList();
        var remainder = total - manualSum;
        var results = new List<Allocation>(entries.Count);

        foreach (var entry in manualEntries)
        {
            results.Add(new Allocation(entry.ProjectId, entry.ManualValue!.Value));
        }

        if (autoEntries.Count == 0)
        {
            return results;
        }

        var equalShare = Money.FromDecimal(remainder / autoEntries.Count).RoundToCurrencyScale().Amount;
        var distributed = 0m;

        for (var index = 0; index < autoEntries.Count; index++)
        {
            var amount = index == autoEntries.Count - 1
                ? Money.FromDecimal(remainder - distributed).RoundToCurrencyScale().Amount
                : equalShare;

            distributed += amount;
            results.Add(new Allocation(autoEntries[index].ProjectId, amount));
        }

        return results;
    }

    public static bool ExcludesGoalBonus(
        Guid projectId,
        IReadOnlyList<ProjectCalculationSnapshot> projectSnapshots) =>
        projectSnapshots.FirstOrDefault(snapshot => snapshot.ProjectId == projectId)?.ExcludesGoalBonus ?? false;
}
