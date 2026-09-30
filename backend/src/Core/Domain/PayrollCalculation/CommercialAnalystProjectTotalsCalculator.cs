using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto do Analista Comercial (REGRAS §6.1).
/// </summary>
public static class CommercialAnalystProjectTotalsCalculator
{
    private const decimal SmallCommissionThreshold = 100m;

    public static Result<IReadOnlyList<ProjectTotalAllocation>> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validation = ComplementAllocationCalculator.ValidateComplementPayingProjects(
            input.ComplementPayingProjects);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(validation.Error!);
        }

        var careerLevel = input.CareerLevel;
        var breakdowns = CommercialAnalystProjectCommissionCalculator.CalculateBreakdowns(
            input.CommercialProjectEntries,
            careerLevel);

        var accumulator = new ProjectTotalsAccumulator();

        for (var index = 0; index < input.CommercialProjectEntries.Count; index++)
        {
            var entry = input.CommercialProjectEntries[index];
            var breakdown = breakdowns[index];
            var grossCommission = breakdown.Commission;
            var platform = careerLevel is null
                ? 0m
                : CommercialAnalystPlatformCalculator.CalculateProject(entry, careerLevel);
            var netCost = Money.FromDecimal(grossCommission - platform).RoundToCurrencyScale().Amount;

            if (grossCommission < SmallCommissionThreshold && input.CommissionPayingProjectId.HasValue)
            {
                accumulator.Add(input.CommissionPayingProjectId.Value, netCost);
            }
            else
            {
                accumulator.Add(entry.ProjectId, netCost);
            }
        }

        var extraFtdBonus = CommercialAnalystCombinedFtdBonusCalculator.CalculateExtra(
            breakdowns,
            careerLevel);
        var betanoInternaValue = careerLevel?.BetanoInternaValue ?? 0m;
        var betanoMundoBetValue = careerLevel?.BetanoMundoBetValue ?? 0m;
        var betanoInternaCommission = Money.FromDecimal(input.BetanoInternaCount * betanoInternaValue)
            .RoundToCurrencyScale()
            .Amount;
        var betanoMundoBetTotal = Money.FromDecimal(input.BetanoMundoBetCount * betanoMundoBetValue)
            .RoundToCurrencyScale()
            .Amount;

        var fullMinimum = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalMinimum = Money.FromDecimal(fullMinimum * factor).RoundToCurrencyScale().Amount;
        var projectCommission = breakdowns.Sum(b => b.Commission);
        var commissionForMinimum = projectCommission + betanoInternaCommission + extraFtdBonus;
        var complement = Math.Max(0m, proportionalMinimum - commissionForMinimum);

        var platformTotal = CommercialAnalystPlatformCalculator.Calculate(
            input.CommercialProjectEntries,
            careerLevel);
        var bucket = complement
            + betanoInternaCommission
            + betanoMundoBetTotal
            + extraFtdBonus
            + (complement > 0m ? platformTotal : 0m);

        if (bucket > 0m)
        {
            var bucketResult = ComplementAllocationCalculator.Allocate(bucket, input);
            if (bucketResult.IsFailure)
            {
                return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(bucketResult.Error!);
            }

            accumulator.AddRange(bucketResult.Value);
        }

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }
}
