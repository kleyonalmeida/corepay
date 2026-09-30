using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Divide <c>supervisor_rev_analista</c> igualmente por todos os projetos (REGRAS §6.2).
/// </summary>
public static class SupervisorRevAnalistaAllocationCalculator
{
    public static IReadOnlyList<FixedAllocationCalculator.Allocation> Calculate(
        decimal supervisorAnalystRevenue,
        IReadOnlyList<SupervisorProjectEntryInput> supervisorProjectEntries)
    {
        if (supervisorAnalystRevenue <= 0m || supervisorProjectEntries.Count == 0)
        {
            return [];
        }

        var equalShare = Money.FromDecimal(supervisorAnalystRevenue / supervisorProjectEntries.Count)
            .RoundToCurrencyScale()
            .Amount;
        var distributed = 0m;
        var results = new List<FixedAllocationCalculator.Allocation>(supervisorProjectEntries.Count);

        for (var index = 0; index < supervisorProjectEntries.Count; index++)
        {
            var amount = index == supervisorProjectEntries.Count - 1
                ? Money.FromDecimal(supervisorAnalystRevenue - distributed).RoundToCurrencyScale().Amount
                : equalShare;

            distributed += amount;
            results.Add(new FixedAllocationCalculator.Allocation(
                supervisorProjectEntries[index].ProjectId,
                amount));
        }

        return results;
    }
}
