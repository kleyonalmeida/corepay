using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Orquestra custo por projeto em períodos de mudança de cargo (REGRAS §5.2).
/// </summary>
public static class RoleChangeProjectTotalsCalculator
{
    public static Result<IReadOnlyList<ProjectTotalAllocation>> Calculate(
        PayrollEntryInput input,
        Func<PayrollEntryInput, Result<IReadOnlyList<ProjectTotalAllocation>>> calcCore)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(calcCore);

        var periodsResult = RoleChangePeriodSplitter.Build(input);
        if (periodsResult.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(periodsResult.Error!);
        }

        var periodTotals = new List<IReadOnlyList<ProjectTotalAllocation>>();
        foreach (var period in periodsResult.Value)
        {
            var periodInput = RoleChangePeriodSplitter.ToPeriodInput(input, period);
            var result = calcCore(periodInput);
            if (result.IsFailure)
            {
                return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(result.Error!);
            }

            periodTotals.Add(result.Value);
        }

        var merged = ProjectTotalsMerger.Merge(periodTotals);
        var accumulator = new ProjectTotalsAccumulator();
        accumulator.AddRange(merged);
        ProjectTotalsMerger.ApplyManualBonuses(accumulator, input.BonusEntries);

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }
}
