using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto do Líder de Projetos: fixo/N + comissão por projeto (REGRAS §6.5).
/// </summary>
public static class ProjectLeaderProjectTotalsCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var accumulator = new ProjectTotalsAccumulator();
        var projectCount = input.ProjectEntries.Count;

        if (projectCount == 0)
        {
            return [];
        }

        var fullBase = BaseSalaryResolver.Resolve(input);
        var factor = ProportionalFactor.CalculateForEntry(input);
        var proportionalBase = Money.FromDecimal(fullBase * factor).RoundToCurrencyScale().Amount;

        var equalShare = Money.FromDecimal(proportionalBase / projectCount).RoundToCurrencyScale().Amount;
        var distributedFixed = 0m;

        for (var index = 0; index < input.ProjectEntries.Count; index++)
        {
            var entry = input.ProjectEntries[index];
            var fixedShare = index == input.ProjectEntries.Count - 1
                ? Money.FromDecimal(proportionalBase - distributedFixed).RoundToCurrencyScale().Amount
                : equalShare;
            distributedFixed += fixedShare;

            var commission = ProjectLeaderCommissionCalculator.CalculateProject(
                entry,
                input.CareerLevel,
                input.Department,
                input.GoalTier);

            accumulator.Add(entry.ProjectId, fixedShare + commission);
        }

        return accumulator.ToList();
    }
}
