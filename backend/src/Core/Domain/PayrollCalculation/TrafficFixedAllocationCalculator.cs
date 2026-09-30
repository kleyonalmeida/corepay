using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Rateio do fixo de tráfego pago — divisão estritamente igualitária (REGRAS §6.3).
/// </summary>
public static class TrafficFixedAllocationCalculator
{
    public static Result<IReadOnlyList<FixedAllocationCalculator.Allocation>> Calculate(
        decimal proportionalFixed,
        IReadOnlyList<RateioProjectEntryInput> rateioProjectEntries)
    {
        if (proportionalFixed <= 0m)
        {
            return Result<IReadOnlyList<FixedAllocationCalculator.Allocation>>.Success([]);
        }

        if (rateioProjectEntries.Count == 0)
        {
            return Result<IReadOnlyList<FixedAllocationCalculator.Allocation>>.Success([]);
        }

        if (rateioProjectEntries.Any(entry => entry.RateioValue.HasValue))
        {
            return Result<IReadOnlyList<FixedAllocationCalculator.Allocation>>.Failure(
                Error.Validation(
                    "traffic.manual_rateio_not_allowed",
                    "Tráfego pago não aceita rateio_value manual; use divisão igualitária entre os projetos."));
        }

        var equalShare = Money.FromDecimal(proportionalFixed / rateioProjectEntries.Count)
            .RoundToCurrencyScale()
            .Amount;
        var distributed = 0m;
        var results = new List<FixedAllocationCalculator.Allocation>(rateioProjectEntries.Count);

        for (var index = 0; index < rateioProjectEntries.Count; index++)
        {
            var amount = index == rateioProjectEntries.Count - 1
                ? Money.FromDecimal(proportionalFixed - distributed).RoundToCurrencyScale().Amount
                : equalShare;

            distributed += amount;
            results.Add(new FixedAllocationCalculator.Allocation(rateioProjectEntries[index].ProjectId, amount));
        }

        return Result<IReadOnlyList<FixedAllocationCalculator.Allocation>>.Success(results);
    }
}
