namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Fator proporcional de admissão/demissão por competência (REGRAS §5.1).
/// Retorna fração decimal entre 0 e 1; arredondamento monetário fica a cargo do caller.
/// </summary>
public static class ProportionalFactor
{
    public static decimal Calculate(
        DateOnly? admissionDate,
        DateOnly? dismissalDate,
        int month,
        int year,
        DateOnly? clipStart = null,
        DateOnly? clipEnd = null)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        if (dismissalDate.HasValue && dismissalDate.Value < monthStart)
        {
            return 0m;
        }

        if (admissionDate.HasValue && admissionDate.Value > monthEnd)
        {
            return 0m;
        }

        var rangeStart = clipStart.HasValue ? Max(monthStart, clipStart.Value) : monthStart;
        var rangeEnd = clipEnd.HasValue ? Min(monthEnd, clipEnd.Value) : monthEnd;

        if (rangeStart > rangeEnd)
        {
            return 0m;
        }

        var admissionInMonth = admissionDate.HasValue
                               && admissionDate.Value >= monthStart
                               && admissionDate.Value <= monthEnd;
        var dismissalInMonth = dismissalDate.HasValue
                               && dismissalDate.Value >= monthStart
                               && dismissalDate.Value <= monthEnd;

        if (admissionInMonth)
        {
            rangeStart = Max(rangeStart, admissionDate!.Value);
        }

        if (dismissalInMonth)
        {
            rangeEnd = Min(rangeEnd, dismissalDate!.Value);
        }

        if (rangeStart > rangeEnd)
        {
            return 0m;
        }

        var daysWorked = rangeEnd.DayNumber - rangeStart.DayNumber + 1;
        return (decimal)daysWorked / daysInMonth;
    }

    public static decimal CalculateForEntry(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Collaborator);

        return Calculate(
            input.Collaborator.AdmissionDate,
            input.Collaborator.DismissalDate,
            input.Month,
            input.Year,
            input.PeriodClipStart,
            input.PeriodClipEnd);
    }

    private static DateOnly Max(DateOnly left, DateOnly right) =>
        left > right ? left : right;

    private static DateOnly Min(DateOnly left, DateOnly right) =>
        left < right ? left : right;
}
