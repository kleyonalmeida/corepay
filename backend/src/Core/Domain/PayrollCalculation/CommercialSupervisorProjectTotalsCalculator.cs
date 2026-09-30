using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto do Supervisor Comercial (REGRAS §6.2).
/// </summary>
public static class CommercialSupervisorProjectTotalsCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var accumulator = new ProjectTotalsAccumulator();
        var careerLevel = input.CareerLevel;

        foreach (var entry in input.SupervisorProjectEntries)
        {
            if (careerLevel is null)
            {
                continue;
            }

            var commission = CommercialSupervisorProjectCommissionCalculator.CalculateProject(
                entry,
                careerLevel);
            accumulator.AddCommission(entry.ProjectId, commission);
        }

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        accumulator.AddRangeFixed(SupervisorFixedAllocationCalculator.Calculate(
            proportionalBase,
            input.SupervisorProjectEntries,
            input.ProjectSnapshots));

        foreach (var allocation in SupervisorRevAnalistaAllocationCalculator.Calculate(
            input.SupervisorAnalystRevenue,
            input.SupervisorProjectEntries))
        {
            accumulator.AddCommission(allocation.ProjectId, allocation.Amount);
        }

        return accumulator.ToList();
    }
}
