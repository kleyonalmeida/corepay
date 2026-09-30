using BuildingBlocks.Results;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Orquestra cálculo por períodos de mudança de cargo (REGRAS §5.2).
/// </summary>
public static class RoleChangePayrollCalculator
{
    public static Result<PayrollEntryResult> Calculate(
        PayrollEntryInput input,
        Func<PayrollEntryInput, Result<PayrollEntryResult>> calcCore)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(calcCore);

        var periodsResult = RoleChangePeriodSplitter.Build(input);
        if (periodsResult.IsFailure)
        {
            return Result<PayrollEntryResult>.Failure(periodsResult.Error!);
        }

        var periodResults = new List<PayrollEntryResult>();
        foreach (var period in periodsResult.Value)
        {
            var periodInput = RoleChangePeriodSplitter.ToPeriodInput(input, period);
            var result = calcCore(periodInput);
            if (result.IsFailure)
            {
                return Result<PayrollEntryResult>.Failure(result.Error!);
            }

            periodResults.Add(result.Value);
        }

        var manualBonuses = ManualAdjustments.SumBonuses(input.BonusEntries);
        var manualDeductions = ManualAdjustments.SumDeductions(input.DeductionEntries);

        return Result<PayrollEntryResult>.Success(
            PayrollEntryResultMerger.Merge(periodResults, manualBonuses, manualDeductions));
    }

    public static bool HasRoleChangesInMonth(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var monthStart = new DateOnly(input.Year, input.Month, 1);
        var monthEnd = new DateOnly(
            input.Year,
            input.Month,
            DateTime.DaysInMonth(input.Year, input.Month));

        return input.RoleChanges.Any(change =>
            change.ChangeDate >= monthStart && change.ChangeDate <= monthEnd);
    }
}
