using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Distribui comissão de grupo Tipster por <see cref="ProjectEntryInput.GroupPercentage"/> (REGRAS §6.9).
/// </summary>
public static class TipsterGroupCommissionAllocationCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var totalGroupPercentage = input.ProjectEntries.Sum(entry => entry.GroupPercentage);
        if (totalGroupPercentage <= 0m)
        {
            return [];
        }

        var perPercent = input.CareerLevel?.GroupCommissionPerPercent ?? 0m;
        var perTwentyPercent = input.CareerLevel?.GroupCommissionPer20Percent ?? 0m;

        var perPercentTotal = totalGroupPercentage * perPercent;
        var vipBands = decimal.Floor(totalGroupPercentage / 20m);
        var vipTotal = vipBands * perTwentyPercent;

        var eligible = input.ProjectEntries
            .Where(entry => entry.GroupPercentage > 0m)
            .ToList();

        var results = new List<ProjectTotalAllocation>();
        var distributedPerPercent = 0m;
        var distributedVip = 0m;

        for (var index = 0; index < eligible.Count; index++)
        {
            var entry = eligible[index];
            var weight = entry.GroupPercentage / totalGroupPercentage;
            var perPercentShare = index == eligible.Count - 1
                ? Money.FromDecimal(perPercentTotal - distributedPerPercent).RoundToCurrencyScale().Amount
                : Money.FromDecimal(perPercentTotal * weight).RoundToCurrencyScale().Amount;
            var vipShare = index == eligible.Count - 1
                ? Money.FromDecimal(vipTotal - distributedVip).RoundToCurrencyScale().Amount
                : Money.FromDecimal(vipTotal * weight).RoundToCurrencyScale().Amount;

            distributedPerPercent += perPercentShare;
            distributedVip += vipShare;

            var amount = perPercentShare + vipShare;
            if (amount != 0m)
            {
                results.Add(new ProjectTotalAllocation(entry.ProjectId, amount));
            }
        }

        return results;
    }
}
