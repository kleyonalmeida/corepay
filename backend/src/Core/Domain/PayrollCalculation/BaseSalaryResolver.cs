namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Resolve o salário-base integral da entrada (snapshot > nível).
/// </summary>
public static class BaseSalaryResolver
{
    public static decimal Resolve(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.FullBaseSalary.HasValue)
        {
            return input.FullBaseSalary.Value;
        }

        return input.CareerLevel?.BaseSalary ?? 0m;
    }
}
