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
            accumulator.Add(entry.ProjectId, commission);
        }

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        accumulator.AddRange(SupervisorFixedAllocationCalculator.Calculate(
            proportionalBase,
            input.SupervisorProjectEntries,
            input.ProjectSnapshots));

        accumulator.AddRange(SupervisorRevAnalistaAllocationCalculator.Calculate(
            input.SupervisorAnalystRevenue,
            input.SupervisorProjectEntries));

        return accumulator.ToList();
    }
}
