using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto do Tráfego Pago: comissão automática + rateio do fixo (REGRAS §6.3).
/// </summary>
public static class PaidTrafficProjectTotalsCalculator
{
    public static Result<IReadOnlyList<ProjectTotalAllocation>> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var accumulator = new ProjectTotalsAccumulator();

        foreach (var projectEntry in input.TrafficProjectEntries)
        {
            var commissionResult = TrafficProjectCommissionCalculator.CalculateProject(
                projectEntry,
                input.CareerLevel,
                input.TrafficSeniorLevel);

            if (commissionResult.IsFailure)
            {
                return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(commissionResult.Error!);
            }

            accumulator.Add(projectEntry.ProjectId, commissionResult.Value);
        }

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = BuildingBlocks.ValueObjects.Money
            .FromDecimal(fullBase * factor)
            .RoundToCurrencyScale()
            .Amount;

        var allocationResult = TrafficFixedAllocationCalculator.Calculate(
            proportionalBase,
            input.RateioProjectEntries);

        if (allocationResult.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(allocationResult.Error!);
        }

        accumulator.AddRange(allocationResult.Value);
        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }
}
