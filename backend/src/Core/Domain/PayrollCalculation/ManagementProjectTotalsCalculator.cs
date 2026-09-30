namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Custo por projeto da Gerência — somente breakdown manual informado (REGRAS §6.4).
/// </summary>
public static class ManagementProjectTotalsCalculator
{
    public static IReadOnlyList<ProjectTotalAllocation> Calculate(PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var accumulator = new ProjectTotalsAccumulator();

        foreach (var revenueEntry in input.ManagementRevenueEntries)
        {
            foreach (var breakdown in revenueEntry.ProjectBreakdown)
            {
                accumulator.Add(breakdown.ProjectId, breakdown.Amount);
            }
        }

        return accumulator.ToList();
    }
}
